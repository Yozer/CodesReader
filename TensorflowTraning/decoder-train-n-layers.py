import numpy as np
import tensorflow as tf
from tensorflow.contrib import rnn
from collections import namedtuple

WIDTH = 475
HEIGHT = 32
OUTPUT_LEN = 29
VOCAB = ['-', '2', '3', '4', '6', '7', '8', '9', 'B', 'C', 'D', 'F', 'G',
         'H', 'J', 'K', 'M', 'P', 'Q', 'R', 'T', 'V', 'W', 'X', 'Y', '<GO>']
VOCAB_SIZE = len(VOCAB)
GO_INDEX = VOCAB.index('<GO>')
GO_ONEHOT = np.eye(VOCAB_SIZE, dtype=np.float32)[GO_INDEX]


def decode(x):
    if isinstance(x, (np.ndarray, np.generic, list)):
        if len(x) != 0 and isinstance(x[0], (np.ndarray, np.generic, list)):
            return [''.join(VOCAB[i] for i in sx) for sx in x]
        return ''.join(VOCAB[i] for i in x)
    return ''


def one_hot_encode(x, classes):
    return np.eye(classes, dtype=np.float32)[x]


def append_go(x):
    return np.c_[[GO_INDEX] * len(x), x]


def normalize(x):
    return x.astype(np.float32) / 255.


def load_data():
    import h5py
    # with h5py.File('codes_data.h5', 'r') as file:
    with h5py.File('codes_gen.h5', 'r') as file:
        data, labels = file['data'][:], file['label'][:]

    shuffle = np.random.permutation(len(data))
    data_ = data[shuffle]
    del data
    labels_ = labels[shuffle]
    del labels

    return data_, labels_


def find_next_dir():
    import os
    from shutil import copy2
    path = 'summary/experiment-{}'
    nr = 0
    while os.path.isdir(path.format(nr)):
        nr += 1
    path = path.format(nr)
    os.makedirs(path + '/models')
    for f in ['decoder-train-n-layers.py']:
        copy2(f, path)
    return path


def build_model(graph, state_size=512, lstm_layers=2):
    with graph.as_default():
        x = tf.placeholder(tf.float32, shape=[None, HEIGHT, WIDTH])
        y = tf.placeholder(tf.float32, shape=[None, OUTPUT_LEN + 1, VOCAB_SIZE])

        with tf.name_scope('convolutions'):
            h = tf.expand_dims(x, axis=3)
            h = tf.layers.conv2d(h, 32, (3, 3), strides=(2, 2), activation=tf.nn.elu)
            # h = tf.contrib.layers.layer_norm(h, activation_fn=tf.nn.relu)
            h = tf.layers.conv2d(h, 32, (3, 3), strides=(2, 2), activation=tf.nn.elu)
            # h = tf.contrib.layers.layer_norm(h, activation_fn=tf.nn.relu)
            h = tf.layers.conv2d(h, 32, (3, 3), strides=(1, 2), activation=tf.nn.elu)
            # h = tf.contrib.layers.layer_norm(h, activation_fn=tf.nn.relu)
            h = tf.layers.conv2d(h, 64, (3, 3), strides=(1, 2), activation=tf.nn.elu)
            # h = tf.contrib.layers.layer_norm(h, activation_fn=tf.nn.relu)
            h = tf.layers.conv2d(h, 64, (3, 3), strides=(1, 1), activation=tf.nn.elu)
            # h = tf.contrib.layers.layer_norm(h, activation_fn=tf.nn.relu)
            h = tf.contrib.layers.flatten(h)
            h = [tf.layers.dense(h, state_size, activation=tf.nn.elu) for _ in range(lstm_layers)]
            # h = [tf.contrib.layers.layer_norm(tf.layers.dense(h, state_size), activation_fn=tf.nn.relu)
            #      for _ in range(lstm_layers)]

        with tf.name_scope('connection'):
            rnn_inputs = y[:, :-1, :]

        with tf.variable_scope('lstm'):
            cell = rnn.LSTMCell(state_size)  # one layer for now
            cell = rnn.MultiRNNCell([cell] * lstm_layers)
            init_state = tuple(rnn.LSTMStateTuple(h[i], h[i]) for i in range(lstm_layers))

        rnn_outputs, _ = tf.nn.dynamic_rnn(cell, rnn_inputs, initial_state=init_state, scope='lstm')

        with tf.variable_scope('softmax'):
            output_weights = tf.Variable(tf.truncated_normal(shape=[state_size, VOCAB_SIZE]))
            output_biases = tf.Variable(tf.zeros(shape=[VOCAB_SIZE]))

            rnn_outputs = tf.reshape(rnn_outputs, [-1, state_size])
            y_ = tf.reshape(y[:, 1:, :], [-1, VOCAB_SIZE])

            logits = tf.matmul(rnn_outputs, output_weights) + output_biases
            loss = tf.reduce_mean(tf.nn.softmax_cross_entropy_with_logits(logits=logits, labels=y_))

            global_step = tf.Variable(0)
            learning_rate = tf.train.exponential_decay(0.001, global_step, decay_steps=5000,
                                                       decay_rate=0.7, staircase=True)
            train_step = tf.train.AdamOptimizer(learning_rate=learning_rate).minimize(loss, global_step)

        with tf.name_scope('summary'):
            train_summary = tf.summary.merge([
                tf.summary.scalar('loss', loss),
                tf.summary.scalar('learning_rate', learning_rate)
            ])
            accuracy_input = tf.placeholder(tf.float32, shape=[2])
            accuracy_tensor = tf.Variable(tf.zeros(shape=[2]))
            with tf.control_dependencies([tf.assign(accuracy_tensor, accuracy_input)]):
                accuracy_summary = tf.summary.merge([
                    tf.summary.scalar('code_accuracy', accuracy_tensor[0]),
                    tf.summary.scalar('letter_accuracy', accuracy_tensor[1])
                ])

        with tf.name_scope('prediction'):
            state = tuple(rnn.LSTMStateTuple(h[i], h[i]) for i in range(lstm_layers))
            nb_batch = tf.shape(x)[0]
            pred = tf.tile(GO_ONEHOT[None, :], multiples=[nb_batch, 1])

            outputs = []
            for i in range(OUTPUT_LEN):
                with tf.variable_scope('lstm', reuse=True):
                    output, state = cell(pred, state)
                pred = tf.nn.softmax(tf.matmul(output, output_weights) + output_biases)
                outputs += [tf.expand_dims(tf.argmax(pred, axis=1), axis=1)]
            prediction = tf.concat(outputs, axis=1)

        tf.add_to_collection('input', x)
        tf.add_to_collection('prediction', prediction)

    return namedtuple('ModelVars', ('x', 'y', 'prediction', 'train_step', 'train_summary',
                                    'accuracy_summary', 'accuracy_input', 'accuracy_tensor'))(
        x=x, y=y, prediction=prediction, train_step=train_step, train_summary=train_summary,
        accuracy_summary=accuracy_summary, accuracy_input=accuracy_input, accuracy_tensor=accuracy_tensor
    )


def main():
    train_x, train_y = load_data()
    valid_x = train_x[-1000:]
    valid_y = train_y[-1000:]
    train_x = train_x[:-1000]
    train_y = train_y[:-1000]

    correct = decode(valid_y)

    nb_epoch = 50
    batch_size = 32

    graph = tf.Graph()
    vs = build_model(graph, state_size=256, lstm_layers=2)

    steps = 0
    with tf.Session(graph=graph) as sess:
        sess.run(tf.global_variables_initializer())
        saver = tf.train.Saver()
        path = find_next_dir()
        train_writer = tf.summary.FileWriter(path, graph=graph, flush_secs=10)
        for epoch in range(nb_epoch):
            print('\nEpoch {}'.format(epoch + 1), flush=True)
            for batch in range(0, len(train_x) - batch_size, batch_size):
                steps += 1

                batch_x = train_x[batch: batch + batch_size]
                batch_y = train_y[batch: batch + batch_size]
                batch_x = normalize(batch_x)
                batch_y = one_hot_encode(append_go(batch_y), VOCAB_SIZE)

                if steps % 50 == 0:
                    s, _ = sess.run([vs.train_summary, vs.train_step],
                                    feed_dict={vs.x: batch_x,
                                               vs.y: batch_y})
                    train_writer.add_summary(s, global_step=steps)
                else:
                    sess.run([vs.train_step],
                             feed_dict={vs.x: batch_x,
                                        vs.y: batch_y})

                print('\r[{:5d}/{:5d}]'.format(batch + batch_size, len(train_x)), end='', flush=True)

            print('\n===== Validate =====')
            [pred] = sess.run([vs.prediction],
                              feed_dict={vs.x: normalize(valid_x)})
            predicted = decode(pred)
            for i in range(min(10, len(valid_x))):
                print('{:3d} -> {} == {} | ({:2d}) {}'.format(i + 1, correct[i], predicted[i],
                                                              sum(x != y for x, y in zip(correct[i], predicted[i])),
                                                              correct[i] == predicted[i]))
            correct_codes = sum(x == y for x, y in zip(correct, predicted)) / 1000.
            correct_letters = sum(x == y for x, y in zip(''.join(correct), ''.join(predicted))) / (1000. * 29.)
            s, _ = sess.run([vs.accuracy_summary, vs.accuracy_tensor],
                            feed_dict={vs.accuracy_input: [correct_codes, correct_letters]})
            train_writer.add_summary(s, global_step=steps)
            print('Correct codes: {:.3f} | Correct letters: {:.5f}'.format(correct_codes, correct_letters))

            saver.save(sess, path + '/models/model', global_step=steps)

if __name__ == '__main__':
    main()
