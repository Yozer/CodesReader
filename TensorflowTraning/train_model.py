import tensorflow as tf
import numpy as np
import h5py
from collections import namedtuple
from scipy.misc import imrotate
from scipy.ndimage import zoom

import matplotlib.pyplot as plt
import seaborn
import random


def augment_data(x, y):
    xs = [x]
    ys = [y]

    nm = 1
    sp = x.shape
    xx = np.empty(shape=(sp[0] * nm, sp[1], sp[2]))
    for i, g in enumerate(x):
        for j in range(nm):
            zm = zoom(g, zoom=1.2)
            xb = random.randint(0, zm.shape[0] - 25)
            yb = random.randint(0, zm.shape[1] - 15)
            xx[i*nm + j] = zm[xb: xb+25, yb: yb+15]

    xs += [xx]
    ys += [y] * nm

    return np.concatenate(xs, axis=0), np.concatenate(ys, axis=0)


def normalize_data(x, y):
    # ms = np.mean(np.mean(x, axis=1), axis=1)
    # x -= ms[:, None, None]
    return x, y


def shuffle_data(x, y):
    shuffle = np.random.permutation(len(x))
    return x[shuffle], y[shuffle]


def load_data(filename='train_data.h5'):
    with h5py.File(filename, 'r') as file:
        data = file['data'][:]
        target = file['target'][:]
        mapping = file['mapping'][:]

    classes = len(mapping)
    target = np.eye(classes)[target]

    return data / 255., target, mapping


def build_model(graph, image_shape, output_shape):
    with graph.as_default():
        x = tf.placeholder(tf.float32, shape=[None] + image_shape)
        labels = tf.placeholder(tf.float32, shape=[None, output_shape])

        h = tf.reshape(x, shape=[-1] + image_shape + [1])

        h = tf.layers.conv2d(h, filters=32, kernel_size=(3, 3), strides=(2, 2), padding='SAME', activation=tf.nn.elu)
        h = tf.layers.conv2d(h, filters=64, kernel_size=(3, 3), strides=(2, 2), padding='SAME', activation=tf.nn.elu)
        h = tf.layers.conv2d(h, filters=64, kernel_size=(3, 3), strides=(2, 2), padding='SAME', activation=tf.nn.elu)
        h = tf.layers.conv2d(h, filters=128, kernel_size=(3, 3), strides=(1, 1), padding='SAME', activation=tf.nn.elu)

        h = tf.layers.conv2d(h, filters=output_shape, kernel_size=(1, 1), strides=(1, 1), activation=tf.nn.elu)
        h = tf.reduce_mean(h, axis=1)
        logits = tf.reduce_mean(h, axis=1)

        prediction = tf.nn.softmax(logits)

        tf.add_to_collection('input', x)
        tf.add_to_collection('prediction', prediction)

        loss = tf.reduce_mean(tf.nn.softmax_cross_entropy_with_logits(logits=logits, labels=labels))

        gl_step = tf.Variable(0)
        learning_rate = tf.train.exponential_decay(0.001, gl_step, 2 * 1500, 0.8, staircase=True)

        train_step = tf.train.AdamOptimizer(learning_rate=learning_rate).minimize(loss, global_step=gl_step)

        accuracy = tf.reduce_mean(tf.cast(tf.equal(tf.argmax(labels, axis=1),
                                                   tf.argmax(prediction, axis=1)),
                                          tf.float32))

        with tf.name_scope('summary'):
            summary = tf.summary.merge([
                tf.summary.scalar('loss', loss),
                tf.summary.scalar('accuracy', accuracy),
                tf.summary.scalar('learning_rate', learning_rate)
            ])

        with tf.name_scope('validation'):
            valid_acc = tf.placeholder(tf.float32)
            valid_acc_tensor = tf.Variable(0.)
            with tf.control_dependencies([tf.assign(valid_acc_tensor, valid_acc)]):
                valid_summary = tf.summary.scalar('validation_accuracy', valid_acc_tensor)

        return namedtuple('Variables', ['input', 'logits', 'prediction', 'labels', 'summary',
                                        'valid_summary', 'valid_acc', 'valid_acc_tensor', 'train_step'])(
            x, logits, prediction, labels, summary, valid_summary, valid_acc, valid_acc_tensor, train_step
        )


def find_next_dir():
    import os
    from shutil import copy2
    path = 'summary/experiment-{}'
    nr = 0
    while os.path.isdir(path.format(nr)):
        nr += 1
    path = path.format(nr)
    os.makedirs(path + '/models')
    copy2('train_model.py', path)
    return path


def main():
    print('Loading data...')
    valid_data, valid_target, _ = load_data('data-3/validation_set.h5')
    data, target, mapping = load_data('data-3/training_set.h5')
    print('Normalize data...')
    data, target = normalize_data(data, target)
    valid_data, valid_target = normalize_data(valid_data, valid_target)
    print('Augmenting data...')
    data, target = augment_data(data, target)
    # valid_data, valid_target = augment_data(valid_data, valid_target)
    print('Shuffling data...')
    data, target = shuffle_data(data, target)

    nb_epoch = 5
    batch_size = 32

    graph = tf.Graph()
    vs = build_model(graph, list(data.shape[1:]), len(mapping))

    with tf.Session(graph=graph) as sess:
        sess.run(tf.global_variables_initializer())
        path = find_next_dir()
        train_writer = tf.summary.FileWriter(path, graph=graph)
        saver = tf.train.Saver()

        global_step = 0
        for e in range(nb_epoch):
            print('\nEpoch {}'.format(e + 1))
            for b in range(0, len(data), batch_size):
                print('\r[{:5d}/{:5d}]'.format(b + batch_size, len(data)), end='')
                s, _ = sess.run([vs.summary, vs.train_step],
                                feed_dict={vs.input: data[b: min(len(data), b + batch_size)],
                                           vs.labels: target[b: min(len(data), b + batch_size)]})
                train_writer.add_summary(s, global_step=global_step)
                global_step += 1
            saver.save(sess, save_path=path + '/models/model', global_step=global_step)

            # validation run
            correct = 0
            for b in range(0, len(valid_data), batch_size):
                valid_x = valid_data[b: min(b + batch_size, len(valid_data))]
                valid_y = valid_target[b: min(b + batch_size, len(valid_data))]

                pred = sess.run(vs.prediction,
                                feed_dict={vs.input: valid_x})

                c_mask = np.isclose(np.argmax(valid_y, axis=1), np.argmax(pred, axis=1))
                correct += np.sum(c_mask)
            valid_accuracy = correct / len(valid_data)
            print('\nValid acc: {:9.7f} ({:5d}/{:5d})'.format(valid_accuracy, correct, len(valid_data)))
            s, _ = sess.run([vs.valid_summary, vs.valid_acc_tensor], feed_dict={vs.valid_acc: valid_accuracy})
            train_writer.add_summary(s, global_step=global_step)


if __name__ == '__main__':
    main()
