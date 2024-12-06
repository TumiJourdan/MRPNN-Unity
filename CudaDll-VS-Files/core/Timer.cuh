#ifndef TIMER_H
#define TIMER_H

#include <string>
#include <deque>
#include <chrono>

// Timer class definition
class Timer {
public:
    Timer(size_t window_size = 100);

    // Start timing
    void start();

    // Stop timing and update rolling average
    void stop();

    // Function to save the current rolling average to a file
    void saveAverageToFile(const std::string& filename) const;

    // Getter for the current rolling average
    double rollingAverage() const;

private:
    size_t window_size;
    std::deque<double> timings;  // Stores the last `window_size` timings
    double rolling_sum;  // Sum of the current timings in the buffer
    std::chrono::high_resolution_clock::time_point start_time;

    // Update the rolling average with a new timing
    void updateRollingAverage(double new_timing);
};

#endif // TIMER_H