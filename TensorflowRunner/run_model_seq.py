import tensorflow as tf
import ctypes as C
import numpy as np
from scipy.misc import imresize
from collections import namedtuple

Model = namedtuple('Model', ['input', 'prediction'])
model = None
sess = tf.Session()


def load_model(path):
    global model
    saver = tf.train.import_meta_graph(path + '.meta')
    saver.restore(sess, path)

    model = Model(
        input=tf.get_collection('input')[0],
        prediction=tf.get_collection('prediction')[0]
    )


def predict(x, size):
    if model is None:
        raise Exception('Model need to be loaded first.')
    x = C.cast(x,C.POINTER(C.c_float))
    x = np.ctypeslib.as_array(x,shape=(size,))
    mapping = ['-', '2', '3', '4', '6', '7', '8', '9', 'B', 'C', 'D', 'F', 'G',
               'H', 'J', 'K', 'M', 'P', 'Q', 'R', 'T', 'V', 'W', 'X', 'Y', '<GO>']

    x = np.reshape(x, newshape=[-1] + model.input.get_shape().as_list()[1:]) / 255.
    flatten = lambda l: [item for sublist in l for item in sublist]
    pred = sess.run(model.prediction, feed_dict={model.input: x})
    return flatten([''.join([mapping[z] for z in code]) for code in pred])
