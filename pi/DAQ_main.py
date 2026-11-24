from modes.MQTT import ModeMQTT
from multiprocessing import Queue
import queue
import random
import time
from datetime import datetime, timezone

from pid_controller import PIDController

HARDWARE_CONNECTED = True


class dummySensor:
    def getReading(self, *args, **kwargs):
        return 23.67 + random.gauss(0, 4)

    def close(self, *args, **kwargs):
        return


class dummyPwm:
    def start(self, *args, **kwargs):
        return

    def stop(self, *args, **kwargs):
        return

    def ChangeDutyCycle(self, *args, **kwargs):
        return


class dummyGPIO:
    IN = 0
    OUT = 1
    LOW = 0
    HIGH = 1
    BCM = 2
    PUD_UP = 3

    def setmode(self, *args, **kwargs):
        return

    def setup(self, *args, **kwargs):
        return

    def PWM(self, *args, **kwargs):
        return dummyPwm()

    def input(self, *args, **kwargs):
        return self.HIGH

    def cleanup(self, *args, **kwargs):
        return


if HARDWARE_CONNECTED:
    import RPi.GPIO as GPIO
    from sensorCommunication.I2C import I2C
    from sensorCommunication.spi import SpiInterface
else:
    GPIO = dummyGPIO()


# GPIO pin used for PWM output (temperature regulation actuator, e.g. heater/fan)
PWM_PIN = 18
PWM_FREQUENCY_HZ = 1000


# Default PID parameters and setpoint; adjust dynamically later as needed.
pid = PIDController(kp=2.0, ki=0.1, kd=0.0, setpoint=25.0)


def main():
    command_queue = Queue(maxsize=10)
    mqtt = ModeMQTT(command_queue)

    # Choose either I2C or SPI sensor. using I2C here.
    if HARDWARE_CONNECTED:
        #sensor = I2C()
        sensor = SpiInterface()
        sensorname = "SPI temperature sensor"
    else:
        sensor = dummySensor()
        sensorname = "Dummy temperature sensor"

    # Raspberry Pi GPIO initialization
    GPIO.setmode(GPIO.BCM)
    GPIO.setup(PWM_PIN, GPIO.OUT)

    pwm = GPIO.PWM(PWM_PIN, PWM_FREQUENCY_HZ)
    pwm.start(0)

    try:
        while True:
            while not command_queue.empty():
                try:
                    message = command_queue.get_nowait()
                except queue.Empty:
                    break

                if isinstance(message, dict):
                    setpoint = message.get("Setpoint", message.get("setpoint"))
                    if setpoint is not None:
                        try:
                            pid.setpoint = float(setpoint)
                        except (TypeError, ValueError):
                            pass

            val = sensor.getReading()

            duty_cycle = pid.update(val)

            # Control PWM pin duty cycle to regulate temperature toward pid.setpoint
            pwm.ChangeDutyCycle(duty_cycle)

            mqtt.publishMessage({
                "Temperature": str(val),
                "Setpoint": str(pid.setpoint),
                "PWM_DutyCycle": str(duty_cycle),
                "PID": {
                    "kp": pid.kp,
                    "ki": pid.ki,
                    "kd": pid.kd,
                },
                "Sensor": sensorname,
                "timestamp": datetime.now(timezone.utc).isoformat(timespec="milliseconds")
            })

            time.sleep(0.5)

    except KeyboardInterrupt:
        pass

    finally:
        sensor.close()
        pwm.stop()
        GPIO.cleanup()


if __name__ == "__main__":
    main()