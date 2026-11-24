
import spidev


class SpiInterface:

    filterConstant = 0.9 # (0.0, 1.0]
    
    def __init__(self):
        self.spi = spidev.SpiDev()
        self.spi.open(0,0)
        self.spi.max_speed_hz = 500000
        self.spi.mode = 0
        self.signal = self._scaleSignal(self._getRawSensorValue())

    def __del__(self):
        self.close()

    def close(self):
        self.spi.close()

    def _getRawSensorValue(self):
        rx = self.spi.xfer2([0x68, 0x00])

        raw = ((rx[0] & 0x03) << 8) | rx[1]
        return raw


    @staticmethod
    def _scaleSignal(raw):
        ADC_MAX = 1023
        V_max = 5
        V_min = 1
        voltage = raw / ADC_MAX * (V_max - V_min) + V_min
        Temp_min = 0
        Temp_max = 50
        temp = voltage / (V_max-V_min) * (Temp_max-Temp_min) + Temp_min
        return temp

    def filterSignal(self, signal):
        a = self.filterConstant
        return a*signal + (1-a)*self.signal

    def getReading(self):
        signal = self._getRawSensorValue()
        signal = self._scaleSignal(signal)
        self.signal = self.filterSignal(signal)
        return self.signal