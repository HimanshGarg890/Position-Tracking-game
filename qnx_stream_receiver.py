import socket
import cv2
import numpy as np

# --- Configuration ---
WIDTH = 288
HEIGHT = 162
HOST = '0.0.0.0'
PORT = 12345
WINDOW_NAME = 'QNX Packed BGR Stream'

def recv_all(sock, n):
    """Helper function to receive n bytes from a socket."""
    data = bytearray()
    while len(data) < n:
        packet = sock.recv(n - len(data))
        if not packet:
            return None
        data.extend(packet)
    return data

def main():
    print(f"✅ Packed BGR Frame Receiver")
    server_socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    server_socket.bind((HOST, PORT))
    server_socket.listen(1)
    print(f"✅ Server listening on {HOST}:{PORT} for {WIDTH}x{HEIGHT} 3-byte BGR frames...")

    conn, addr = server_socket.accept()
    print(f"✅ Connection established with {addr}")

    # Frame size is now 3 bytes per pixel
    frame_size = HEIGHT * WIDTH * 3
    cv2.namedWindow(WINDOW_NAME, cv2.WINDOW_NORMAL)

    try:
        while True:
            frame_data = recv_all(conn, frame_size)
            if not frame_data:
                print("Connection closed by client.")
                break

            # Reshape directly into a 3-channel BGR image
            frame = np.frombuffer(frame_data, dtype=np.uint8).reshape((HEIGHT, WIDTH, 3))
            
            cv2.imshow(WINDOW_NAME, frame)

            if cv2.waitKey(1) & 0xFF == ord('q'):
                break
    finally:
        print("Closing connection and shutting down.")
        conn.close()
        server_socket.close()
        cv2.destroyAllWindows()

if __name__ == '__main__':
    main()
