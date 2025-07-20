/*
 * camera_streamer.c
 *
 * Fastest possible TCP version. Skips every other frame to reduce CPU load
 * and disables Nagle's algorithm to reduce network latency.
 */
#include <stdio.h>
#include <stdlib.h>
#include <unistd.h>
#include <string.h>
#include <sys/types.h>
#include <sys/socket.h>
#include <netinet/in.h>
#include <netinet/tcp.h> // For TCP_NODELAY
#include <netdb.h>
#include <termios.h>
#include <camera/camera_api.h>

#define SERVER_IP "10.33.32.247"
#define SERVER_PORT 12345
#define DOWNSCALE_WIDTH 96
#define DOWNSCALE_HEIGHT 54
#define DOWNSCALE_FACTOR 24

static int g_sock = -1;
static uint32_t* g_small_frame_buffer = NULL;

void downscale_frame(const camera_buffer_t* big_frame, uint32_t* small_buffer) {
    uint32_t src_width = big_frame->framedesc.bgr8888.width;
    const uint32_t* src_buf = (const uint32_t*)big_frame->framebuf;
    for (int y = 0; y < DOWNSCALE_HEIGHT; ++y) {
        for (int x = 0; x < DOWNSCALE_WIDTH; ++x) {
            small_buffer[y * DOWNSCALE_WIDTH + x] = src_buf[(y * DOWNSCALE_FACTOR) * src_width + (x * DOWNSCALE_FACTOR)];
        }
    }
}

static void processCameraData(camera_handle_t handle, camera_buffer_t* buffer, void* arg) {
    static int frame_counter = 0;
    if (g_sock < 0 || g_small_frame_buffer == NULL) return;

    // --- NEW: Process only every other frame ---
    if (++frame_counter % 2 != 0) {
        return;
    }

    if (buffer->frametype == CAMERA_FRAMETYPE_BGR8888) {
        downscale_frame(buffer, g_small_frame_buffer);
        uint32_t small_frame_size = DOWNSCALE_WIDTH * DOWNSCALE_HEIGHT * 4;
        write(g_sock, g_small_frame_buffer, small_frame_size);
    }
}

int main(void) {
    camera_handle_t handle = CAMERA_HANDLE_INVALID;
    struct sockaddr_in server;
    struct hostent *hp;

    g_small_frame_buffer = malloc(DOWNSCALE_WIDTH * DOWNSCALE_HEIGHT * 4);
    if (!g_small_frame_buffer) {
        fprintf(stderr, "Failed to allocate memory.\n");
        return EXIT_FAILURE;
    }

    printf("Connecting to %s:%d...\n", SERVER_IP, SERVER_PORT);
    g_sock = socket(AF_INET, SOCK_STREAM, 0);
    hp = gethostbyname(SERVER_IP);
    server.sin_family = AF_INET;
    memcpy(&server.sin_addr, hp->h_addr, hp->h_length);
    server.sin_port = htons(SERVER_PORT);
    if (connect(g_sock, (struct sockaddr *)&server, sizeof(server)) < 0) {
        perror("ERROR connecting");
        free(g_small_frame_buffer);
        return EXIT_FAILURE;
    }

    // --- NEW: Disable Nagle's algorithm ---
    int flag = 1;
    if (setsockopt(g_sock, IPPROTO_TCP, TCP_NODELAY, (char *)&flag, sizeof(int)) < 0) {
        perror("Could not set TCP_NODELAY");
    }

    printf(" -> Connected.\n");

    if (camera_open(CAMERA_UNIT_1, CAMERA_MODE_RW, &handle) != CAMERA_EOK) {
        fprintf(stderr, "Failed to open camera.\n");
        free(g_small_frame_buffer);
        return EXIT_FAILURE;
    }
    printf(" -> Camera opened.\n");

    if (camera_start_viewfinder(handle, &processCameraData, NULL, NULL) != CAMERA_EOK) {
        fprintf(stderr, "Failed to start viewfinder.\n");
        camera_close(handle);
        free(g_small_frame_buffer);
        return EXIT_FAILURE;
    }

    printf("\nStreaming... Press any key to stop.\n");
    struct termios oldterm, newterm;
    tcgetattr(STDIN_FILENO, &oldterm);
    newterm = oldterm;
    newterm.c_lflag &= ~(ECHO | ICANON);
    tcsetattr(STDIN_FILENO, TCSANOW, &newterm);
    read(STDIN_FILENO, NULL, 1);
    tcsetattr(STDIN_FILENO, TCSANOW, &oldterm);

    printf("\nStopping...\n");
    camera_stop_viewfinder(handle);
    camera_close(handle);
    close(g_sock);
    free(g_small_frame_buffer);
    return EXIT_SUCCESS;
}