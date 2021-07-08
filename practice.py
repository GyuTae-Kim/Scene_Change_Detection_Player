# 원정 추가
import cv2
import numpy as np
import sys


videoCapture = cv2.VideoCapture(sys.argv[1])

i = 0
_, frame = videoCapture.read()

width, height, _ = frame.shape
difference = list()

while True:
    if(videoCapture.get(cv2.CAP_PROP_POS_FRAMES) + int(sys.argv[3]) >= videoCapture.get(cv2.CAP_PROP_FRAME_COUNT)): break
    i += int(sys.argv[3])
    videoCapture.set(1, i)

    current_frame = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)
    _, frame = videoCapture.read()
    previous_frame = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)
    
    frame_diff = cv2.absdiff(current_frame, previous_frame)

    prob = np.array(frame_diff)
    if(int(sys.argv[2]) <= prob.sum() / (width * height)) : difference.append(i - int(sys.argv[3]))

    
    
with open("frames.ini", 'w') as file:
    for i in difference:
        file.write('{}\n'.format(i))

exit(0)