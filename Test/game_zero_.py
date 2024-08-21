# 게임번호 0번입니다 (약지 소지 번갈아펴기)

import socket
import cv2
import mediapipe as mp
import time
import json

# UDT 소켓 설정
sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
# sendport = ('192.168.1.10', 5034)    # Wi-Fi IPv4 주소
sendport = ('127.0.0.1', 5034)    # Wi-Fi IPv4 주소

# MediaPipe 손 인식 모듈 초기화
mp_hands = mp.solutions.hands
mp_drawing = mp.solutions.drawing_utils
drawing_spec1 = mp_drawing.DrawingSpec(thickness=2, circle_radius=4, color=(0,255,0))
drawing_spec2 = mp_drawing.DrawingSpec(thickness=2, color=(0,0,255))

def get_hand_pose(hand_landmarks, handedness):
    result_finger = []
    LR = 1000 if handedness.classification[0].label == "Right" else 2000

    # 엄지손가락
    thumb_tip = hand_landmarks.landmark[4]
    thumb_ip = hand_landmarks.landmark[3]
    if (LR == 1000 and thumb_tip.x > thumb_ip.x) or (LR == 2000 and thumb_tip.x < thumb_ip.x):
        result_finger.append((LR+4, "open"))
    else:
        result_finger.append((LR+4, "close"))

    # 나머지 손가락
    for i in [
        # 8, 12, 16, 
        20]:
        tip = hand_landmarks.landmark[i]
        pip = hand_landmarks.landmark[i-2]
        if tip.y < pip.y:
            result_finger.append((LR+i, "open"))
        else:
            result_finger.append((LR+i, "close"))

    return result_finger

# 정답 정의
# 총 10 스테이지
right_answers = [ # 2~ 왼손 1~ 오른손
    [[(2004, "open"),(2020, "close")],
     [(1004, "close"),(1020, "open")]],

    [[(2004, "close"),(2020, "open")],
     [(1004, "open"),(1020, "close")]],


    [[(2004, "open"),(2020, "close")],
     [(1004, "close"),(1020, "open")]],


    [[(2004, "close"),(2020, "open")],
     [(1004, "open"),(1020, "close")]],


    [[(2004, "open"),(2020, "close")],
     [(1004, "close"),(1020, "open")]],

    [[(2004, "close"),(2020, "open")],
     [(1004, "open"),(1020, "close")]],


    [[(2004, "open"),(2020, "close")],
     [(1004, "close"),(1020, "open")]],

    [[(2004, "close"),(2020, "open")],
     [(1004, "open"),(1020, "close")]],


    [[(2004, "open"),(2020, "close")],
     [(1004, "close"),(1020, "open")]],

    [[(2004, "close"),(2020, "open")],
     [(1004, "open"),(1020, "close")]],
]

def send_game_state():
    json_data = json.dumps(game_state)
    sock.sendto(json_data.encode(), sendport)
# 웹캠 설정
cap = cv2.VideoCapture(0)
game_state = {
    'game_index': '0',
    'current_stage' : '0',
    'stage_complete' : 'False',
    'final_complete': 'False',
}
current_stage = 0
stage_complete = False
stage_complete_time = 0
success_message_duration = 1  # 성공 메시지 표시 시간을 1초로 변경

with mp_hands.Hands(
    max_num_hands=2,
    min_detection_confidence=0.5,
    min_tracking_confidence=0.5,
) as hands:
    while cap.isOpened():
        res, image = cap.read()
        if not res:
            print("웹캠에서 이미지를 가져오는 것을 실패했습니다")
            break

        image = cv2.cvtColor(cv2.flip(image, 1), cv2.COLOR_BGR2RGB)
        
        image.flags.writeable = False
        results = hands.process(image)
        image.flags.writeable = True
        image = cv2.cvtColor(image, cv2.COLOR_RGB2BGR)

        left_hand_state = []
        right_hand_state = []

        if results.multi_hand_landmarks:
            for hand_landmarks, handedness in zip(results.multi_hand_landmarks, results.multi_handedness):
                mp_drawing.draw_landmarks(
                    image, hand_landmarks, mp_hands.HAND_CONNECTIONS,
                    drawing_spec1,
                    drawing_spec2
                )
                finger_states = get_hand_pose(hand_landmarks, handedness)

                for idx, (finger, state) in enumerate(finger_states):
                    if finger // 1000 == 2:
                        LR, fin = 'Left', finger % 100
                        cv2.putText(image, f"{LR} Finger {fin}: {state}", (10, 30 + idx * 30), 
                                    cv2.FONT_HERSHEY_SIMPLEX, 0.7, (255,0,0), 2)
                        left_hand_state.append((finger, state))
                    if finger // 1000 == 1:
                        LR, fin = 'Right', finger % 100
                        cv2.putText(image, f"{LR} Finger {fin}: {state}", (image.shape[1] - 300, 30 + idx * 30), 
                                    cv2.FONT_HERSHEY_SIMPLEX, 0.7, (0,0,255), 2)
                        right_hand_state.append((finger, state))

        # 현재 단계의 정답과 비교
        if left_hand_state and right_hand_state:
            if current_stage < len(right_answers):
                if (left_hand_state == right_answers[current_stage][0] and 
                    right_hand_state == right_answers[current_stage][1]):
                    if not stage_complete:
                        stage_complete = True
                        stage_complete_time = time.time()
                        current_stage += 1
                        game_state['current_stage'] = str(current_stage)
                        game_state['stage_complete'] = 'True'
                        send_game_state()
                    cv2.putText(image, f"Stage {current_stage}", (image.shape[1] - 300, image.shape[0] - 30), 
                                cv2.FONT_HERSHEY_SIMPLEX, 1, (0,255,0), 2)
                else:
                    stage_complete = False
                    # game_state['current_stage'] = str(current_stage)
                    # game_state['stage_complete'] = 'False'
                    # send_game_state()

        # 성공 메시지 표시
        if stage_complete and time.time() - stage_complete_time < success_message_duration:
            cv2.putText(image, "success!", (int(image.shape[1]/2) - 50, int(image.shape[0]/2)), 
                        cv2.FONT_HERSHEY_SIMPLEX, 2, (0,255,0), 3)
            # 성공 메시지 표시 후 stage_complete를 False로 변경
            game_state['stage_complete'] = 'False'
            send_game_state()
            # if time.time() - stage_complete_time >= success_message_duration:

        # 모든 단계 완료 시 메시지 표시
        if current_stage == len(right_answers):
            end_time = time.time() + 2  # 현재 시간 + 2초
            while time.time() < end_time:
                _, frame = cap.read()
                frame = cv2.flip(frame, 1)
                cv2.putText(frame, "All steps completed!", (int(frame.shape[1]/2) - 150, int(frame.shape[0]/2)), 
                            cv2.FONT_HERSHEY_SIMPLEX, 1.5, (0,255,0), 3)
                cv2.imshow("Hand Tracking", frame)
                cv2.waitKey(1)
            
            game_state['final_complete'] = 'True'
            send_game_state()
            break

        cv2.imshow("Hand Tracking", image)
        if cv2.waitKey(1) == ord('q'):
            break

cap.release()
cv2.destroyAllWindows()
sock.close()

