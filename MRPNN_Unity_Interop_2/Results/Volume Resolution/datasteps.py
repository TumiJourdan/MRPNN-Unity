import math

def stepfunction():
    for x in range(10):
        result = 1024*(math.pow(0.125,x/9))
        print(f"Step : {math.floor(result)}")

stepfunction()