import cv2
import mediapipe as mp
import numpy as np
import socket
import json
import time

# UDP 소켓 설정
sock = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
unity_address = ('127.0.0.1', 5052)  # Unity의 수신 포트

# MediaPipe Hand 모델 초기화
mp_hands = mp.solutions.hands
hands = mp_hands.Hands(static_image_mode=False, max_num_hands=2, min_detection_confidence=0.5, min_tracking_confidence=0.5)

def landmark_to_dict(landmark):
    return {"x": landmark.x, "y": landmark.y, "z": landmark.z}

# 웹캠 설정
cap = cv2.VideoCapture(0)
cap.set(cv2.CAP_PROP_FRAME_WIDTH, 640)  # 해상도를 줄임
cap.set(cv2.CAP_PROP_FRAME_HEIGHT, 480)

# FPS 제한 설정
fps_limit = 30
frame_time = 1 / fps_limit

print("핸드 트래킹이 실행 중입니다. 종료하려면 'q'를 누르세요.")

try:
    prev_frame_time = time.time()
    while cap.isOpened():
        current_time = time.time()
        
        # FPS 제한
        if current_time - prev_frame_time < frame_time:
            continue

        success, image = cap.read()
        if not success:
            print("웹캠을 찾을 수 없습니다.")
            continue
        image = cv2.cvtColor(cv2.flip(image, 1), cv2.COLOR_BGR2RGB)

        # 이미지를 BGR에서 RGB로 변환 (MediaPipe는 RGB를 사용)
        image = cv2.cvtColor(image, cv2.COLOR_BGR2RGB)
        
        # 손 감지 수행
        results = hands.process(image)
        
        hands_data = {"left": None, "right": None}
        
        if results.multi_hand_landmarks:
            for hand_landmarks, handedness in zip(results.multi_hand_landmarks, results.multi_handedness):
                # 랜드마크 데이터를 딕셔너리 리스트로 변환
                landmarks_list = [landmark_to_dict(lm) for lm in hand_landmarks.landmark]
                
                # 왼손/오른손 결정
                hand_side = "left" if handedness.classification[0].label == "Left" else "right"
                
                # 데이터 딕셔너리에 할당
                hands_data[hand_side] = {"landmarks": landmarks_list}

        # Unity로 데이터 전송
        json_data = json.dumps(hands_data)
        sock.sendto(json_data.encode(), unity_address)

        # FPS 계산 및 출력
        current_time = time.time()
        fps = 1 / (current_time - prev_frame_time)
        prev_frame_time = current_time
        print(f"FPS: {fps:.2f}", end="\r")

        # 'q'를 누르면 종료
        if cv2.waitKey(1) & 0xFF == ord('q'):
            break

except KeyboardInterrupt:
    print("\n프로그램을 종료합니다...")

finally:
    cap.release()
    cv2.destroyAllWindows()
    sock.close()