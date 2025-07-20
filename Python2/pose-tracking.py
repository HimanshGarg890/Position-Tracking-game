import cv2
import mediapipe as mp
import numpy as np
import time as t
import json
import asyncio
import websockets
import threading

# WebSocket configuration
WEBSOCKET_HOST = "localhost"
WEBSOCKET_PORT = 8765
connected_clients = set()

async def handler(websocket):  # Removed 'path' parameter
    print(f"Client connected from {websocket.remote_address}")
    connected_clients.add(websocket)
    
    # Send gui_ready message immediately when client connects
    try:
        await websocket.send("gui_ready")
        print("Sent gui_ready message to client")
    except Exception as e:
        print(f"Error sending gui_ready: {e}")
    
    try:
        # Keep the connection alive by listening for incoming messages
        async for message in websocket:
            # Handle incoming messages (like ping/pong for heartbeat)
            if message == "ping":
                await websocket.send("pong")
                print("Responded to ping with pong")
            else:
                print(f"Received from client: {message}")
                
    except websockets.exceptions.ConnectionClosed:
        print("Client disconnected normally")
    except Exception as e:
        print(f"Client connection error: {e}")
    finally:
        if websocket in connected_clients:
            connected_clients.remove(websocket)
        print(f"Client disconnected from {websocket.remote_address}")
        print(f"Remaining clients: {len(connected_clients)}")

async def broadcast_data(data):
    if connected_clients:
        message = json.dumps(data)
        disconnected = []
        for client in connected_clients.copy():
            try:
                await client.send(message)
            except websockets.exceptions.ConnectionClosed:
                disconnected.append(client)
            except Exception as e:
                print(f"Error sending to client: {e}")
                disconnected.append(client)
        
        # Clean up disconnected clients
        for client in disconnected:
            connected_clients.discard(client)
            print(f"Removed disconnected client, remaining: {len(connected_clients)}")

mp_drawing = mp.solutions.drawing_utils
mp_pose = mp.solutions.pose

# Jump detection variables
prev_left_foot_y = None
prev_right_foot_y = None
foot_velocity_threshold = 0.01  # Minimum y-change per frame to count as jump
jump_cooldown = 0.5  # seconds
last_jump_time = 0
velocity_window = 3  # frames to average velocity over
left_foot_velocities = []
right_foot_velocities = []

# Punch detection state variables
left_punching = False
right_punching = False

# Action detection functions
def detect_step(landmarks):
    right_knee_y = landmarks[mp_pose.PoseLandmark.RIGHT_KNEE].y
    right_hip_y = landmarks[mp_pose.PoseLandmark.RIGHT_HIP].y
    return right_knee_y < right_hip_y

# Kick detection state variables
last_kick_time = 0
kick_cooldown = 0.5  # seconds
kicking_state = False
normal_ankle_distance = None
KICK_DISTANCE_THRESHOLD = 1.5  # multiplier for normal distance to detect kick

def detect_kick(landmarks, current_time):
    global last_kick_time, kicking_state, normal_ankle_distance
    
    left_ankle = landmarks[mp_pose.PoseLandmark.LEFT_ANKLE]
    right_ankle = landmarks[mp_pose.PoseLandmark.RIGHT_ANKLE]
    
    # Calculate current ankle distance using both horizontal and vertical components
    current_distance = np.sqrt(
        (left_ankle.x - right_ankle.x)**2 + 
        (left_ankle.y - right_ankle.y)**2
    )
    
    # Initialize or update normal ankle distance during standing
    if normal_ankle_distance is None or not kicking_state:
        if abs(left_ankle.y - right_ankle.y) < 0.1:  # Feet roughly level
            normal_ankle_distance = current_distance
    
    # Return false if we don't have a baseline yet
    if normal_ankle_distance is None:
        return False
    
    # Check if current distance is significantly larger than normal
    kick_detected = current_distance > (normal_ankle_distance * KICK_DISTANCE_THRESHOLD)
    
    # Handle cooldown and state
    if kick_detected and not kicking_state and (current_time - last_kick_time) > kick_cooldown:
        kicking_state = True
        last_kick_time = current_time
        
        # Get hip positions to determine kick direction
        left_hip_z = landmarks[mp_pose.PoseLandmark.LEFT_HIP].z
        right_hip_z = landmarks[mp_pose.PoseLandmark.RIGHT_HIP].z
        
        # Determine kick direction based on which hip is closer to the camera
        if right_hip_z < left_hip_z:  # Right hip is closer (smaller z value)
            print("Left Kick!")  # Kicking towards the left
        else:
            print("Right Kick!")  # Kicking towards the right
        return True
    elif not kick_detected:
        kicking_state = False
    
    return False

def detect_punch(left_wrist, right_wrist, left_hip, right_hip, body_center_x, punch_threshold=0.1):
    global left_punching, right_punching

    # Right arm punch detection
    right_punch_now = right_hip.y - right_wrist.y > punch_threshold
    if right_punch_now and not right_punching:
        if right_wrist.x < body_center_x:
            print("Punch to the LEFT (right hand)")
        else:
            print("Punch to the RIGHT (right hand)")
        right_punching = True
    elif not right_punch_now:
        right_punching = False

    # Left arm punch detection
    left_punch_now = left_hip.y - left_wrist.y > punch_threshold
    if left_punch_now and not left_punching:
        if left_wrist.x < body_center_x:
            print("Punch to the LEFT (left hand)")
        else:
            print("Punch to the RIGHT (left hand)")
        left_punching = True
    elif not left_punch_now:
        left_punching = False

def detect_jump(current_left_y, current_right_y, current_time):
    global prev_left_foot_y, prev_right_foot_y, left_foot_velocities, right_foot_velocities, last_jump_time
    jump_detected = False

    if prev_left_foot_y is not None:
        left_velocity = prev_left_foot_y - current_left_y  # positive when moving up
        right_velocity = prev_right_foot_y - current_right_y

        left_foot_velocities.append(left_velocity)
        right_foot_velocities.append(right_velocity)

        if len(left_foot_velocities) > velocity_window:
            left_foot_velocities.pop(0)
            right_foot_velocities.pop(0)

        if len(left_foot_velocities) == velocity_window:
            avg_left_velocity = np.mean(left_foot_velocities)
            avg_right_velocity = np.mean(right_foot_velocities)

            if (current_time - last_jump_time > jump_cooldown and
                avg_left_velocity > foot_velocity_threshold and 
                avg_right_velocity > foot_velocity_threshold):
                print("JUMP!")
                last_jump_time = current_time
                jump_detected = True

    prev_left_foot_y = current_left_y
    prev_right_foot_y = current_right_y
    return jump_detected


# --- Walking detection state (reverted to original logic) ---
prev_left_knee_y = None
prev_right_knee_y = None
prev_left_ankle_y = None
prev_right_ankle_y = None
walking_state = False
walking_cooldown = 0
WALKING_COOLDOWN_FRAMES = 5  # Number of frames to keep walking after motion stops
MOTION_THRESHOLD = 0.025      # Minimum movement to count as motion (original value)
walking_direction = None  # 'left', 'right', or None

def detect_walking(landmarks):
    global prev_left_knee_y, prev_right_knee_y, prev_left_ankle_y, prev_right_ankle_y
    global walking_state, walking_cooldown, walking_direction

    left_knee_y = landmarks[mp_pose.PoseLandmark.LEFT_KNEE].y
    right_knee_y = landmarks[mp_pose.PoseLandmark.RIGHT_KNEE].y
    left_ankle_y = landmarks[mp_pose.PoseLandmark.LEFT_ANKLE].y
    right_ankle_y = landmarks[mp_pose.PoseLandmark.RIGHT_ANKLE].y

    # Only use knees and ankles for walking detection
    if prev_left_knee_y is None:
        prev_left_knee_y = left_knee_y
        prev_right_knee_y = right_knee_y
        prev_left_ankle_y = left_ankle_y
        prev_right_ankle_y = right_ankle_y
        walking_direction = None
        return False

    left_knee_motion = abs(left_knee_y - prev_left_knee_y)
    right_knee_motion = abs(right_knee_y - prev_right_knee_y)
    left_ankle_motion = abs(left_ankle_y - prev_left_ankle_y)
    right_ankle_motion = abs(right_ankle_y - prev_right_ankle_y)

    # Only consider leg movement for walking
    motion_detected = (
        (left_knee_motion > MOTION_THRESHOLD or right_knee_motion > MOTION_THRESHOLD or
         left_ankle_motion > MOTION_THRESHOLD or right_ankle_motion > MOTION_THRESHOLD)
    )

    if motion_detected:
        walking_state = True
        walking_cooldown = WALKING_COOLDOWN_FRAMES
    else:
        if walking_cooldown > 0:
            walking_cooldown -= 1
        else:
            walking_state = False

    prev_left_knee_y = left_knee_y
    prev_right_knee_y = right_knee_y
    prev_left_ankle_y = left_ankle_y
    prev_right_ankle_y = right_ankle_y

    left_hip_z = landmarks[mp_pose.PoseLandmark.LEFT_HIP].z
    right_hip_z = landmarks[mp_pose.PoseLandmark.RIGHT_HIP].z
    if walking_state:
        if right_hip_z < left_hip_z:  # Right hip is closer (smaller z value)
            walking_direction = 'left'   # Walking towards the left
        elif left_hip_z < right_hip_z:  # Left hip is closer (smaller z value)
            walking_direction = 'right'  # Walking towards the right
        else:
            walking_direction = None
    else:
        walking_direction = None

    return walking_state

# Global event loop for thread communication
loop = None

def run_camera_processing():
    """Run camera processing in synchronous code"""
    global loop
    
    cap = cv2.VideoCapture(0)
    print("Camera initialized")

    if connected_clients:
        asyncio.run_coroutine_threadsafe(
        broadcast_data({"type": "gui_ready"}), loop
        )

    with mp_pose.Pose(min_detection_confidence=0.5, min_tracking_confidence=0.5) as pose:
        while cap.isOpened():
            ret, frame = cap.read()
            if not ret:
                break

            # Convert image to RGB
            image = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)
            image.flags.writeable = False

            # Process the image and detect the pose
            results = pose.process(image)

            # Convert back to BGR for display
            image.flags.writeable = True
            image = cv2.cvtColor(image, cv2.COLOR_RGB2BGR)

            # Initialize values
            walk_value = 0.0
            kick_value = 0.0
            punch_value = 0
            move_value = 0.0
            jump_detected = False

            if results.pose_landmarks:
                landmarks = results.pose_landmarks.landmark
                mp_drawing.draw_landmarks(
                    image, results.pose_landmarks, mp_pose.POSE_CONNECTIONS)

                current_time = t.time()
                if detect_kick(landmarks, current_time):
                    cv2.putText(image, "Kick!", (50, 150), cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 0, 255), 2)

                # --- Punch Detection ---
                left_wrist = landmarks[mp_pose.PoseLandmark.LEFT_WRIST]
                right_wrist = landmarks[mp_pose.PoseLandmark.RIGHT_WRIST]
                left_hip = landmarks[mp_pose.PoseLandmark.LEFT_HIP]
                right_hip = landmarks[mp_pose.PoseLandmark.RIGHT_HIP]
                body_center_x = (left_hip.x + right_hip.x) / 2
                detect_punch(left_wrist, right_wrist, left_hip, right_hip, body_center_x)

                # --- Jump Detection ---
                left_ankle = landmarks[mp_pose.PoseLandmark.LEFT_ANKLE]
                right_ankle = landmarks[mp_pose.PoseLandmark.RIGHT_ANKLE]

                jump_detected = detect_jump(left_ankle.y, right_ankle.y, current_time)
                if jump_detected:
                    cv2.putText(image, "Jump!", (50, 250), cv2.FONT_HERSHEY_SIMPLEX, 1, (0, 255, 255), 2)

                if detect_walking(landmarks):
                    direction_str = ""
                    if walking_direction == 'left':
                        direction_str = " Left"
                    elif walking_direction == 'right':
                        direction_str = " Right"
                    cv2.putText(image, f"Walking!{direction_str}", (50, 300), cv2.FONT_HERSHEY_SIMPLEX, 1, (128, 0, 128), 2)

                # Prepare data for JSON
                if walking_direction == 'left':
                    walk_value = -1.0
                elif walking_direction == 'right':
                    walk_value = 1.0

                # Determine kick direction
                if kicking_state:
                    left_hip_z = landmarks[mp_pose.PoseLandmark.LEFT_HIP].z
                    right_hip_z = landmarks[mp_pose.PoseLandmark.RIGHT_HIP].z
                    if right_hip_z < left_hip_z:  # Right hip closer, kicking left
                        kick_value = 1.0
                    else:  # Left hip closer, kicking right
                        kick_value = -1.0

                # Determine punch direction
                if left_punching:
                    if left_wrist.x < body_center_x:
                        punch_value = 1  # Left punch
                    else:
                        punch_value = -1  # Right punch
                elif right_punching:
                    if right_wrist.x < body_center_x:
                        punch_value = 1  # Left punch
                    else:
                        punch_value = -1  # Right punch

                # Movement value (same as walking for now)
                move_value = walk_value

            # Display image
            cv2.imshow('MediaPipe Pose', image)

            # Prepare data to send
            data = {
                "jump": bool(jump_detected),
                "walk": walk_value,
                "kick": kick_value,
                "punch": punch_value,
                "move": move_value
            }

            # Debug print of all JSON elements
            #print(f"DEBUG: jump={data['jump']}, walk={data['walk']}, kick={data['kick']}, punch={data['punch']}, move={data['move']}")

            # Write data to input_data.json
            try:
                with open("input_data.json", "w") as f:
                    json.dump(data, f)
            except Exception as e:
                print(f"Error writing to input_data.json: {e}")

            # Send data over WebSocket (schedule it on the event loop)
            if loop and not loop.is_closed():
                try:
                    asyncio.run_coroutine_threadsafe(broadcast_data(data), loop)
                except Exception as e:
                    print(f"Error sending data: {e}")

            if cv2.waitKey(10) & 0xFF == ord('q'):
                break

    cap.release()
    cv2.destroyAllWindows()
    print("Camera processing stopped")

async def main():
    global loop
    loop = asyncio.get_event_loop()
    
    # Updated server creation for newer websockets library
    server = await websockets.serve(handler, WEBSOCKET_HOST, WEBSOCKET_PORT)
    print(f"WebSocket server started on ws://{WEBSOCKET_HOST}:{WEBSOCKET_PORT}")
    
    # Start camera processing in a separate thread
    camera_thread = threading.Thread(target=run_camera_processing, daemon=True)
    camera_thread.start()
    print("Camera processing started in separate thread")
    
    try:
        # Keep the server running
        await server.wait_closed()
    except KeyboardInterrupt:
        print("Server stopping...")
    finally:
        server.close()

if __name__ == "__main__":
    try:
        asyncio.run(main())
    except KeyboardInterrupt:
        print("Application stopped by user")
    except Exception as e:
        print(f"Error: {e}")
        input("Press Enter to exit...")