#include <iostream>
#include <fstream>
#include <string>

#include <cuda.h>
#include <d3d11.h>
#include <cuda_runtime.h>
#include <cuda_d3d11_interop.h>
#include "IUnityGraphics.h"
#include "IUnityInterface.h"
#include "IUnityGraphicsD3D11.h"
#include <curand_kernel.h>

#include "device_launch_parameters.h"
#include "volume.hpp"

#include <thread>

using namespace std;
#define CUDA_CHECK(err) checkCudaError(err, __FILE__, __LINE__)

inline void checkCudaError(cudaError_t err, const char* file, int line) {
    if (err != cudaSuccess) {
        std::cerr << "CUDA error at " << file << ":" << line << " - "
            << cudaGetErrorString(err) << std::endl;
        std::cerr << "Press Enter to exit..." << std::endl;
        std::cin.get(); // Wait for the user to press Enter
        exit(err); // Exit if there's an error
    }
}

class Logger {
public:
    Logger(const std::string& filename) {
        file.open(filename, std::ios::out | std::ios::app);  // Open in append mode
        if (!file) {
            std::cerr << "Failed to open log file: " << filename << std::endl;
        }
    }

    ~Logger() {
        if (file.is_open()) {
            file.close();
        }
    }

    void log(const std::string& message) {
        if (file.is_open()) {
            file << message << std::endl;
            file.flush();  // Force data to write to disk
        }
    }

    // Overload to log numeric values
    void log(float value) {
        if (file.is_open()) {
            file << value << std::endl;
            file.flush();  // Force data to write to disk
        }
    }
    void logArray(float* array, int width, int height) {
        if (file.is_open()) {
            for (int i = 0; i < height; ++i) {
                for (int j = 0; j < width; ++j) {
                    file << array[i * width + j]; // Access the element in a 1D manner
                    if (j < width - 1) {
                        file << ","; // Add a comma if not the last element in the row
                    }
                }
                file << std::endl; // New line after each row
            }
            file.flush(); // Force data to write to disk
        }
    }

private:
    std::ofstream file;
};

// Function pointer for Unity's texture update callback
static IUnityInterfaces* s_UnityInterfaces = nullptr;
static IUnityGraphics* s_Graphics = nullptr;
static UnityGfxRenderer s_RendererType = kUnityGfxRendererNull;
static ID3D11Device* g_D3D11Device = nullptr;
static IDXGIAdapter* g_DXGIAdapter = nullptr;
ID3D11DeviceContext* deviceContext = nullptr;
ID3D11Query* query;

// Function pointer for Unity's texture update callback
static void(UNITY_INTERFACE_API* UpdateTextureFromUnity)(void* textureHandle) = nullptr;

// CUDA graphics resource
vector<tuple<ID3D11Texture2D*, cudaGraphicsResource*, cudaSurfaceObject_t>> cudaResources;
cudaGraphicsResource* infoResource;


// Logger (assuming you have a logger class)
Logger logger("Cuda_Unity_Direct.txt");
// Graphics device event callback
static void UNITY_INTERFACE_API OnGraphicsDeviceEvent(UnityGfxDeviceEventType eventType)
{
    switch (eventType)
    {
    case kUnityGfxDeviceEventInitialize:
    {
        s_RendererType = s_Graphics->GetRenderer();
        if (s_RendererType == kUnityGfxRendererD3D11)
        {
            g_D3D11Device = s_UnityInterfaces->Get<IUnityGraphicsD3D11>()->GetDevice();
            if (g_D3D11Device)
            {
                IDXGIDevice* dxgiDevice = nullptr;
                if (SUCCEEDED(g_D3D11Device->QueryInterface(__uuidof(IDXGIDevice), (void**)&dxgiDevice)))
                {
                    dxgiDevice->GetAdapter(&g_DXGIAdapter);
                    dxgiDevice->Release();
                }

                //Query setup
                D3D11_QUERY_DESC queryDesc;
                ZeroMemory(&queryDesc, sizeof(queryDesc));
                queryDesc.Query = D3D11_QUERY_EVENT;
                HRESULT result = g_D3D11Device->CreateQuery(&queryDesc, &query);
                if (FAILED(result)) {
                    logger.log("Failed to make query");
                }

                g_D3D11Device->GetImmediateContext(&deviceContext);

            }
            logger.log("D3D11 device and DXGI Adapter initialized");
        }
        break;
    }
    case kUnityGfxDeviceEventShutdown:
    {
        s_RendererType = kUnityGfxRendererNull;
        if (g_DXGIAdapter)
        {
            g_DXGIAdapter->Release();
            g_DXGIAdapter = nullptr;
        }
        g_D3D11Device = nullptr;
        break;
    }
    case kUnityGfxDeviceEventBeforeReset:
    case kUnityGfxDeviceEventAfterReset:
        // Handle these events if necessary
        break;
    };
}

// Plugin load event
extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginLoad(IUnityInterfaces * unityInterfaces)
{
    logger.log("Loading");
    s_UnityInterfaces = unityInterfaces;
    s_Graphics = s_UnityInterfaces->Get<IUnityGraphics>();
    s_Graphics->RegisterDeviceEventCallback(OnGraphicsDeviceEvent);

    // Run OnGraphicsDeviceEvent(initialize) manually on plugin load
    OnGraphicsDeviceEvent(kUnityGfxDeviceEventInitialize);
}
extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API deregister_cuda_resources() {
    for (auto& resourceTuple : cudaResources) {
        cudaGraphicsResource* cudaResource = std::get<1>(resourceTuple);
        //destroy surf
        cudaSurfaceObject_t surfObj = std::get<2>(resourceTuple);
        if (surfObj != 0) {
            cudaError_t err = cudaDestroySurfaceObject(surfObj);
            cudaDeviceSynchronize();
            if (err != cudaSuccess) {
                logger.log("Failed to destroy surface resource: 1 " + std::string(cudaGetErrorString(err)));
            }
        }
        cudaDeviceSynchronize();

        // Unmap the resource if it's mapped
        cudaGraphicsUnmapResources(1, &cudaResource, 0);

        // Deregister the CUDA graphics resource
        cudaError_t err = cudaGraphicsUnregisterResource(cudaResource);
        if (err != cudaSuccess) {
            logger.log("Failed to unregister resource: " + std::string(cudaGetErrorString(err)));
        }
    }

    // Clear the resources vector
    cudaResources.clear();
}
// Plugin unload event
extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API UnityPluginUnload()
{
    logger.log("Starting plugin unload");

    // Do full cleanup including unregistering resources
    deregister_cuda_resources();

    if (s_Graphics != nullptr) {
        s_Graphics->UnregisterDeviceEventCallback(OnGraphicsDeviceEvent);
        logger.log("Graphics device callback unregistered");
    }

    logger.log("Plugin unload completed");
}

// Set texture update callback
extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API SetTextureFromUnity(void* textureHandle)
{
    UpdateTextureFromUnity = (void(UNITY_INTERFACE_API*)(void*))textureHandle;
    logger.log("Stored function pointer for texture update");
}
// Initialize CUDA interop

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API RegisterTexture2D(ID3D11Texture2D * texture) {
    if (!g_DXGIAdapter) {
        logger.log("DXGI Adapter not initialized");
        return;
    }
    // Get the CUDA device corresponding to the D3D11 device
    int cudaDevice;
    cudaError_t err = cudaD3D11GetDevice(&cudaDevice, g_DXGIAdapter);
    if (err != cudaSuccess) {
        logger.log("Failed to get CUDA device: " + std::string(cudaGetErrorString(err)));
        return;
    }

    // Set the CUDA device
    err = cudaSetDevice(cudaDevice);
    if (err != cudaSuccess) {
        logger.log("Failed to set CUDA device: " + std::string(cudaGetErrorString(err)));
        return;
    }

    // Check if the texture is already registered
    for (const auto& resourceTuple : cudaResources) {
        if (std::get<0>(resourceTuple) == texture) {
            logger.log("Texture is already registered with CUDA");
            return;
        }
    }

    // Register the D3D11 texture with CUDA and add to the vector
    cudaGraphicsResource* cudaResource = nullptr;
    err = cudaGraphicsD3D11RegisterResource(&cudaResource, texture, cudaGraphicsRegisterFlagsNone);
    if (err != cudaSuccess) {
        logger.log("Failed to register texture with CUDA: " + std::string(cudaGetErrorString(err)));
        return;
    }

    // Store the newly registered resource in the vector
    cudaResources.emplace_back(texture, cudaResource, 0);
    logger.log("CUDA interop initialized successfully and resource added to vector");
}

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API unmap_cuda_resources() {

    int count = 0;
    for (auto& resourceTuple : cudaResources) {
        cudaGraphicsResource* cudaResource = std::get<1>(resourceTuple);
        // Unmap the CUDA graphics resource
        cudaError_t err1 = cudaGraphicsUnmapResources(1, &std::get<1>(resourceTuple), 0);

        if (err1 != cudaSuccess) {
            logger.log("Failed to unmap resource: 2 " + std::string(cudaGetErrorString(err1)));
            logger.log("Count = " + to_string(count));
        }
        count += 1;

    }

    cudaDeviceSynchronize();
}


// GPU Memory buffers
float3* d_target;
Histogram* d_histo_buffer;
unsigned int* d_target2;

// Volume instance
VolumeRender* volume_inst = nullptr;
int multiScatter = 512;
float g = 0.857;
int randseed = 0;
VolumeRender::RenderType rt = VolumeRender::MRPNN;
int toneType = 2;//ACES tone
bool denoise = true;

//Debug histo
__device__ __managed__ float3 prevOrigin;
__device__ __managed__ float3 prevUp;
__device__ __managed__ float3 prevRight;
__device__ __managed__ float3 prevLight;
__device__ __managed__ float prevAlpha;
__device__ __managed__ float3 prevColour;
extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API initialize_memory(int2 size,int resolution = 512) {


    cudaDeviceSynchronize();
    volume_inst = new VolumeRender(resolution);

    CUDA_CHECK(cudaMalloc((void**)&d_target, size.x * size.y * sizeof(float3)));
    CUDA_CHECK(cudaMalloc((void**)&d_histo_buffer, size.x * size.y * sizeof(Histogram)));
    CUDA_CHECK(cudaMalloc((void**)&d_target2, size.x * size.y * sizeof(unsigned int)));
    prevOrigin = make_float3(0.0f, 0.0f, 0.0f);
    prevUp = make_float3(0.0f, 0.0f, 0.0f);
    prevRight = make_float3(0.0f, 0.0f, 0.0f);
    cudaDeviceSynchronize();
    volume_inst->SetDatas([](int x, int y, int z, float u, float v, float w) {
        float dis = distance(make_float3(0.5f, 0.5f, 0.5f), make_float3(u, v, w));
        return dis < 0.25 ? 1.0f : 0;
        });

    volume_inst->Update(); // Call Update after changing volumetric data.

}

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API initialize_memory_path(int2 size, const char* path, int targetResolution) {
    
    try {
        std::string pathStr(path);
        // Replace all backslashes with forward slashes
        std::replace(pathStr.begin(), pathStr.end(), '\\', '/');

        std::string fullPath = pathStr + ".bin";
        logger.log("Attempting to load model from: " + fullPath);

        FILE* file = fopen(fullPath.c_str(), "rb");
        if (file == nullptr) {
            logger.log("ERROR: Cannot open file at path: " + fullPath);
            logger.log("Error code: " + std::string(strerror(errno)));
            return;
        }
        fclose(file);

        volume_inst = new VolumeRender(pathStr, targetResolution);
    }
    catch (const std::exception& e) {
        logger.log("Exception: " + std::string(e.what()));
    }

    CUDA_CHECK(cudaMalloc((void**)&d_target, size.x * size.y * sizeof(float3)));
    CUDA_CHECK(cudaMalloc((void**)&d_histo_buffer, size.x * size.y * sizeof(Histogram)));
    CUDA_CHECK(cudaMalloc((void**)&d_target2, size.x * size.y * sizeof(unsigned int)));
    prevOrigin = make_float3(0.0f, 0.0f, 0.0f);
    prevUp = make_float3(0.0f, 0.0f, 0.0f);
    prevRight = make_float3(0.0f, 0.0f, 0.0f);
    cudaDeviceSynchronize();


}
extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API set_volume_datas() {


    float3 scatter = float3{ 1, 1, 1 };
    volume_inst->SetScatterRate(scatter);
    volume_inst->UpdateHGLut(0.857);
    volume_inst->SetEnvExp(0);
    volume_inst->SetTrScale(1);
    volume_inst->SetExposure(1);
    volume_inst->SetSurfaceIOR(-1);
    volume_inst->SetCheckboard(true);
}

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API set_raymarch_step(int stepCount) {
    volume_inst->set_Step_Num(stepCount);
}
extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API clean_memory() {
    Logger logger("cleanup.txt");
    logger.log("Starting play mode cleanup");

    cudaError_t err = cudaSuccess;
    volume_inst->saveTimings();
    // Clean up volume instance if it exists
    if (volume_inst != nullptr) {
        delete volume_inst;
        volume_inst = nullptr;
        logger.log("Volume instance cleaned up");
    }

    // Free device memory
    if (d_target != nullptr) {
        err = cudaFree(d_target);
        if (err != cudaSuccess) {
            logger.log("Warning: Failed to free d_target: " + std::string(cudaGetErrorString(err)));
        }
        d_target = nullptr;
    }

    if (d_target2 != nullptr) {
        err = cudaFree(d_target2);
        if (err != cudaSuccess) {
            logger.log("Warning: Failed to free d_target2: " + std::string(cudaGetErrorString(err)));
        }
        d_target2 = nullptr;
    }

    if (d_histo_buffer != nullptr) {
        err = cudaFree(d_histo_buffer);
        if (err != cudaSuccess) {
            logger.log("Warning: Failed to free d_histo_buffer: " + std::string(cudaGetErrorString(err)));
        }
        d_histo_buffer = nullptr;
    }

    // Reset CUDA graphics resource but don't unregister it
    // since we might need it again in the next play session
    unmap_cuda_resources();
    deregister_cuda_resources();
    cudaDeviceSynchronize();
    logger.log("Play mode cleanup completed");
}

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API setHDRIInterface(string path) {
    volume_inst->SetHDRI(path);
}

__host__ __device__ bool hasChanged(float3 a, float3 b) {
    return a.x != b.x || a.y != b.y || a.z != b.z;
}

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API cuda_run_render(float3 origin, float3 up, float3 right, float3 lightDir, float3 lightColor, float alpha, int2 size) {
    //DIRCT3D INTEROP


    cudaError_t err = cudaSuccess;
    cudaDeviceSynchronize();
    
    for (auto& resourceTuple : cudaResources) {
        cudaGraphicsResource* cudaResource = std::get<1>(resourceTuple);

        // Map the resource
        err = cudaGraphicsMapResources(1, &cudaResource, 0);
        if (err != cudaSuccess) {
            logger.log("Failed to map resource: " + std::string(cudaGetErrorString(err)));
            continue;
        }
        cudaDeviceSynchronize();
        // Create the surface object if needed
        cudaSurfaceObject_t& surfObj = std::get<2>(resourceTuple);
        if (surfObj == 0) {
            // Get the CUDA array
            cudaArray* cuArray = nullptr;
            err = cudaGraphicsSubResourceGetMappedArray(&cuArray, cudaResource, 0, 0);
            if (err != cudaSuccess) {
                logger.log("Failed to get mapped array: " + std::string(cudaGetErrorString(err)));
                cudaGraphicsUnmapResources(1, &cudaResource, 0);
                continue;
            }
            cudaDeviceSynchronize();
            cudaResourceDesc resDesc = {};
            resDesc.resType = cudaResourceTypeArray;
            resDesc.res.array.array = cuArray;

            err = cudaCreateSurfaceObject(&surfObj, &resDesc);
            if (err != cudaSuccess) {
                logger.log("Failed to create surface object: " + std::string(cudaGetErrorString(err)));
                cudaGraphicsUnmapResources(1, &cudaResource, 0);
                continue;
            }
            cudaDeviceSynchronize();
        }
    }
    //CALLING THE RENDER FUNCTION


    randseed += 1;
    //reset randseed 
    if (hasChanged(origin, prevOrigin) || hasChanged(up, prevUp) || hasChanged(right, prevRight) || hasChanged(lightDir, prevLight) || hasChanged(prevColour, lightColor) || prevAlpha!=alpha) {
        randseed = 0;
    }
    // Update the previous frame values
    prevOrigin = origin;
    prevUp = up;
    prevRight = right;
    prevLight = lightDir;
    prevAlpha = alpha;
    prevColour = lightColor;
    cudaSurfaceObject_t surfObj = get<2>(cudaResources[0]);
    cudaSurfaceObject_t infoSurfObj = get<2>(cudaResources[1]);

    volume_inst->Render(d_target, d_histo_buffer, d_target2, size, origin, up, right, lightDir, lightColor, alpha, multiScatter, 0.857, randseed, rt, toneType, denoise, surfObj,infoSurfObj);

    //CLEANING DIRECT3D INTEROP
    unmap_cuda_resources();
    CUDA_CHECK(cudaDeviceSynchronize());  // Wait for kernel to complete
}


/*

RESOURCES

unsigned int* h_target2 = nullptr;

// GPU Memory buffers
float3* d_target;
Histogram* d_histo_buffer;
unsigned int* d_target2;
VolumeRender* volume_inst = nullptr;
mapped pointer
registered resource

*/