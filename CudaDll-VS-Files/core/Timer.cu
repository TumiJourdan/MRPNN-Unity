
#include "Timer.cuh"
#include <fstream>
#include <iostream>

Timer::Timer(size_t window_size) : window_size(window_size), rolling_sum(0.0) {}

void Timer::start() {
    start_time = std::chrono::high_resolution_clock::now();
}

void Timer::stop() {
    auto end_time = std::chrono::high_resolution_clock::now();
    double elapsed = std::chrono::duration<double, std::milli>(end_time - start_time).count();
    updateRollingAverage(elapsed);
}

void Timer::saveAverageToFile(const std::string& filename) const {
    std::ofstream outFile(filename);
    if (outFile.is_open()) {
        outFile << "Rolling Average Time (last " << window_size << " calls): " << rollingAverage() << " ms\n";
        outFile.close();
        std::cout << "Rolling average time saved to " << filename << std::endl;
    }
    else {
        std::cerr << "Error: Could not open file " << filename << std::endl;
    }
}

double Timer::rollingAverage() const {
    return timings.empty() ? 0.0 : rolling_sum / timings.size();
}

void Timer::updateRollingAverage(double new_timing) {
    timings.push_back(new_timing);
    rolling_sum += new_timing;

    // If the buffer exceeds the window size, remove the oldest timing
    if (timings.size() > window_size) {
        rolling_sum -= timings.front();
        timings.pop_front();
    }
}