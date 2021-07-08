### IMPORT ###

from keras.applications.vgg16 import preprocess_input, VGG16
from keras.models             import Model

import cv2
import sys
import numpy as np


### MODEL ###

vgg16          = VGG16(weights='imagenet', include_top=True)
extractedVgg16 = Model(inputs=vgg16.input, outputs=vgg16.get_layer('fc1').output)


### TEST ###

videoCapture = cv2.VideoCapture(sys.argv[1])
_, frame = videoCapture.read()
frame = cv2.resize(frame, (224, 224), interpolation=cv2.INTER_CUBIC)
frame = preprocess_input(frame)

outputs = [extractedVgg16.predict(np.array([frame]))]
i = 0

with open("frames.ini", 'w') as file:
    while True:
        if(videoCapture.get(cv2.CAP_PROP_POS_FRAMES) + int(sys.argv[3]) >= videoCapture.get(cv2.CAP_PROP_FRAME_COUNT)): break
        i += int(sys.argv[3])
        videoCapture.set(1, i)

        _, frame = videoCapture.read()
        frame = cv2.resize(frame, (224, 224), interpolation=cv2.INTER_CUBIC)
        frame = preprocess_input(frame)
        outputs.append(extractedVgg16.predict(np.array([frame])))
    
        result = np.sqrt(np.sum(np.power((outputs[i // int(sys.argv[3]) - 1][0] - outputs[i // int(sys.argv[3])][0]), 2)))
        if (int(sys.argv[2]) >= result) : file.write('{}\n'.format(i - int(sys.argv[3])))