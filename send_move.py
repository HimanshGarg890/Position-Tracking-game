import asyncio
import websockets
import cv2
import mediapipe as mp
import time

async def send_movement():
    uri = "ws://localhost:8080"
    async with websockets.connect(uri) as ws:
        cap = cv2.VideoCapture(0)
        mp_pose = mp.solutions.pose.Pose()

        prev_hip_y = None
        prev_time = time.time()
        prev_label = "idle"

        while cap.isOpened():
            success, frame = cap.read()
            if not success:
                break

            frame_rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
            result = mp_pose.process(frame_rgb)

            label = "idle"
            current_time = time.time()

            if result.pose_landmarks:
                lm = result.pose_landmarks.landmark

                # Right Punch Detection
                if lm[16].x < lm[14].x < lm[12].x:
                    label = "punch_right"
                # Left Punch Detection
                elif lm[15].x > lm[13].x > lm[11].x:
                    label = "punch_left"
                # Right Kick
                elif lm[28].y < lm[26].y:
                    label = "kick_right"
                # Left Kick
                elif lm[27].y < lm[25].y:
                    label = "kick_left"
                # Jump (both feet rise significantly)
                elif lm[31].y < 0.4 and lm[32].y < 0.4:
                    label = "jump"
                # Walk/Run Detection (based on hip Y movement)
                elif prev_hip_y is not None:
                    hip_avg_y = (lm[24].y + lm[23].y) / 2
                    dy = abs(hip_avg_y - prev_hip_y)
                    dt = current_time - prev_time
                    speed = dy / dt if dt > 0 else 0

                    if speed > 1.0:
                        label = "run"
                    elif speed > 0.3:
                        label = "walk"

                    prev_hip_y = hip_avg_y
                    prev_time = current_time
                else:
                    prev_hip_y = (lm[24].y + lm[23].y) / 2
                    prev_time = current_time

            # Avoid sending duplicate labels constantly
            if label != prev_label:
                await ws.send(label)
                print("Sent:", label)
                prev_label = label

            cv2.imshow("Pose", frame)
            if cv2.waitKey(5) & 0xFF == 27:
                break

        cap.release()
        await ws.send("exit")

asyncio.run(send_movement())
