import tensorflow as tf
import ctypes as C
import numpy as np
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
def deinit():
    sess.close()

def predict(x, size):
    if model is None:
        raise Exception('Model need to be loaded first.')

    x = C.cast(x,C.POINTER(C.c_float))
    x = np.ctypeslib.as_array(x,shape=(size,))
    mapping = [b'B', b'C', b'D', b'F', b'G', b'H', b'J', b'K', b'M', b'P',
               b'Q', b'R', b'T', b'V', b'W', b'X', b'Y', b'2', b'3', b'4', b'6',
               b'7', b'8', b'9']

    x = np.reshape(x, newshape=[-1] + model.input.get_shape().as_list()[1:]) / 255.
    pred = np.argmax(sess.run(model.prediction, feed_dict={model.input: x}), axis=1)
    return list(map(lambda z: mapping[z], pred))
