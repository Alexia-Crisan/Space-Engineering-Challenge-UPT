import RPi.GPIO as GPIO
import time
import curses

# --- GPIO Pin Setup (based on your wiring) ---
AIN1, AIN2, PWMA = 27, 17, 18
BIN1, BIN2, PWMB = 24, 22, 23
STBY = 25

GPIO.setmode(GPIO.BCM)
for pin in [AIN1, AIN2, PWMA, BIN1, BIN2, PWMB, STBY]:
    GPIO.setup(pin, GPIO.OUT)

GPIO.output(STBY, GPIO.HIGH)

# --- PWM Setup ---
pwmA = GPIO.PWM(PWMA, 1000)
pwmB = GPIO.PWM(PWMB, 1000)
pwmA.start(0)
pwmB.start(0)

speed = 70  # Default speed (0–100)

# --- Movement Functions ---
def forward():
    GPIO.output(AIN1, GPIO.HIGH)
    GPIO.output(AIN2, GPIO.LOW)
    GPIO.output(BIN1, GPIO.HIGH)
    GPIO.output(BIN2, GPIO.LOW)
    pwmA.ChangeDutyCycle(speed)
    pwmB.ChangeDutyCycle(speed)

def backward():
    GPIO.output(AIN1, GPIO.LOW)
    GPIO.output(AIN2, GPIO.HIGH)
    GPIO.output(BIN1, GPIO.LOW)
    GPIO.output(BIN2, GPIO.HIGH)
    pwmA.ChangeDutyCycle(speed)
    pwmB.ChangeDutyCycle(speed)

def left():
    GPIO.output(AIN1, GPIO.LOW)
    GPIO.output(AIN2, GPIO.HIGH)
    GPIO.output(BIN1, GPIO.HIGH)
    GPIO.output(BIN2, GPIO.LOW)
    pwmA.ChangeDutyCycle(speed)
    pwmB.ChangeDutyCycle(speed)

def right():
    GPIO.output(AIN1, GPIO.HIGH)
    GPIO.output(AIN2, GPIO.LOW)
    GPIO.output(BIN1, GPIO.LOW)
    GPIO.output(BIN2, GPIO.HIGH)
    pwmA.ChangeDutyCycle(speed)
    pwmB.ChangeDutyCycle(speed)

def stop():
    pwmA.ChangeDutyCycle(0)
    pwmB.ChangeDutyCycle(0)

def main(stdscr):
    global speed
    stdscr.nodelay(True)
    stdscr.clear()
    stdscr.addstr(0, 0, "Use arrow keys to control. Press 'q' to quit.")

    while True:
        key = stdscr.getch()
        if key == curses.KEY_UP:
            forward()
        elif key == curses.KEY_DOWN:
            backward()
        elif key == curses.KEY_LEFT:
            left()
        elif key == curses.KEY_RIGHT:
            right()
        elif key == ord('q'):
            break
        else:
            stop()

        time.sleep(0.05)

    stop()
    GPIO.cleanup()

curses.wrapper(main)
