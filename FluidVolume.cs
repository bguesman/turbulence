using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.HighDefinition;
using Expanse;

namespace Turbulence {

[ExecuteInEditMode]
public class FluidVolume : MonoBehaviour
{
    // Public params.
    public ProceduralCloudVolume m_renderer;
    [Range(0, 10)]
    public float m_viscosity = 0.1f;
    public float m_gravity = 0;
    public float m_upwardForce = 0;
    [Range(0, 1)]
    public float m_upwardForceThreshold = 0;
    [Min(0)]
    public float m_densitySource = 0;
    public Texture2D m_densitySourceTex = null;

    // Simulation grids.
    RTHandle m_density, m_velocity, m_pressure, m_densityTemp, m_velocityTemp, m_pressureTemp;
    private Vector3Int kResolution = new Vector3Int(256, 256, 256);

    // Compute shader.
    private ComputeShader[] m_CS = new ComputeShader[15];


    // Start is called before the first frame update
    void Start()
    {
        for (int i = 0; i < m_CS.Length; i++) {
            m_CS[i] = Resources.Load<ComputeShader>("Turbulence");
        }
        reallocate(kResolution);
    }

    void OnDestroy() 
    {
        deallocate();
    }

    // Update is called once per frame
    void Update()
    {
        setupRenderer();
        if (m_density != null && m_velocity != null) {
            simulate();
        }
    }

    private void simulate() {
        // Make sure compute shaders are allocated.
        for (int i = 0; i < m_CS.Length; i++) {
            if (m_CS[i] == null) {
                m_CS[i] = Resources.Load<ComputeShader>("Turbulence");
            }
        }

        simulateVelocity();
        simulateDensity();
    }

    private void simulateVelocity() {
        // Advect velocity by itself.
        Graphics.CopyTexture(m_velocity, m_velocityTemp);
        int advectVelocityHandle = m_CS[0].FindKernel("ADVECT_VELOCITY");
        m_CS[0].SetVector("_resV", new Vector4(m_velocity.rt.width, m_velocity.rt.height, m_velocity.rt.volumeDepth, 1));
        m_CS[0].SetTexture(advectVelocityHandle, "_V", m_velocity);
        m_CS[0].SetTexture(advectVelocityHandle, "_V_tex", m_velocityTemp);
        m_CS[0].SetFloat("_dt", Time.deltaTime);
        m_CS[0].Dispatch(advectVelocityHandle, 
            IRenderer.computeGroups(m_velocity.rt.width, 4), 
            IRenderer.computeGroups(m_velocity.rt.height, 4), 
            IRenderer.computeGroups(m_velocity.rt.volumeDepth, 4));
        
        // Add gravity everywhere.
        int addForceHandle = m_CS[0].FindKernel("ADD_FORCE");
        m_CS[0].SetVector("_resD", new Vector4(m_density.rt.width, m_density.rt.height, m_density.rt.volumeDepth, 1));
        m_CS[0].SetVector("_resV", new Vector4(m_velocity.rt.width, m_velocity.rt.height, m_velocity.rt.volumeDepth, 1));
        m_CS[0].SetTexture(addForceHandle, "_V", m_velocity);
        m_CS[0].SetVector("_f", new Vector4(0, m_gravity, 0, 0));
        m_CS[0].SetFloat("_heightThreshold", 1);
        m_CS[0].SetFloat("_dt", Time.deltaTime);
        m_CS[0].Dispatch(addForceHandle, 
            IRenderer.computeGroups(m_velocity.rt.width, 4), 
            IRenderer.computeGroups(m_velocity.rt.height, 4), 
            IRenderer.computeGroups(m_velocity.rt.volumeDepth, 4));
        
        // Add upward force.
        m_CS[1].SetVector("_resD", new Vector4(m_density.rt.width, m_density.rt.height, m_density.rt.volumeDepth, 1));
        m_CS[1].SetVector("_resV", new Vector4(m_velocity.rt.width, m_velocity.rt.height, m_velocity.rt.volumeDepth, 1));
        m_CS[1].SetTexture(addForceHandle, "_V", m_velocity);
        m_CS[1].SetVector("_f", new Vector4(0, m_upwardForce, 0, 0));
        m_CS[1].SetFloat("_heightThreshold", m_upwardForceThreshold);
        m_CS[1].SetFloat("_dt", Time.deltaTime);
        m_CS[1].Dispatch(addForceHandle, 
            IRenderer.computeGroups(m_velocity.rt.width, 4), 
            IRenderer.computeGroups(m_velocity.rt.height, 4), 
            IRenderer.computeGroups(m_velocity.rt.volumeDepth, 4));

        // Diffuse velocity.
        Graphics.CopyTexture(m_velocity, m_velocityTemp);
        int diffuseVelocityHandle = m_CS[0].FindKernel("DIFFUSE_VELOCITY");
        m_CS[0].SetVector("_resV", new Vector4(m_velocity.rt.width, m_velocity.rt.height, m_velocity.rt.volumeDepth, 1));
        m_CS[0].SetTexture(diffuseVelocityHandle, "_V", m_velocity);
        m_CS[0].SetTexture(diffuseVelocityHandle, "_V_tex", m_velocityTemp);
        m_CS[0].SetFloat("_dt", Time.deltaTime);
        m_CS[0].SetFloat("_viscosity", m_viscosity);
        m_CS[0].Dispatch(diffuseVelocityHandle, 
            IRenderer.computeGroups(m_velocity.rt.width, 4), 
            IRenderer.computeGroups(m_velocity.rt.height, 4), 
            IRenderer.computeGroups(m_velocity.rt.volumeDepth, 4));

        // Take pressure steps.
        int clearHandle = m_CS[0].FindKernel("CLEAR_PRESSURE");
        m_CS[0].SetTexture(clearHandle, "_P", m_pressure);
        m_CS[0].Dispatch(clearHandle, 
            IRenderer.computeGroups(m_pressure.rt.width, 4), 
            IRenderer.computeGroups(m_pressure.rt.height, 4), 
            IRenderer.computeGroups(m_pressure.rt.volumeDepth, 4));

        int kPressureSteps = 15;
        for (int i = 0; i < kPressureSteps; i++) {
            int copyHandle = m_CS[i].FindKernel("COPY_PRESSURE");
                m_CS[i].SetTexture(copyHandle, "_P", m_pressureTemp);
                m_CS[i].SetTexture(copyHandle, "_P_tex", m_pressure);
                m_CS[i].Dispatch(copyHandle, 
                    IRenderer.computeGroups(m_pressure.rt.width, 4), 
                    IRenderer.computeGroups(m_pressure.rt.height, 4), 
                    IRenderer.computeGroups(m_pressure.rt.volumeDepth, 4));

            int pressureHandle = m_CS[0].FindKernel("PRESSURE_STEP");
            m_CS[0].SetVector("_resV", new Vector4(m_velocity.rt.width, m_velocity.rt.height, m_velocity.rt.volumeDepth, 1));
            m_CS[0].SetTexture(pressureHandle, "_V_tex", m_velocity);
            m_CS[0].SetTexture(pressureHandle, "_P", m_pressure);
            m_CS[0].SetTexture(pressureHandle, "_P_tex", m_pressureTemp);
            m_CS[0].SetFloat("_dt", Time.deltaTime);
            m_CS[0].Dispatch(pressureHandle, 
                IRenderer.computeGroups(m_pressure.rt.width, 4), 
                IRenderer.computeGroups(m_pressure.rt.height, 4), 
                IRenderer.computeGroups(m_pressure.rt.volumeDepth, 4));
            
            int pressureBoundaryHandle = m_CS[0].FindKernel("IMPOSE_PRESSURE_BOUNDARY");
            m_CS[0].SetVector("_resV", new Vector4(m_velocity.rt.width, m_velocity.rt.height, m_velocity.rt.volumeDepth, 1));
            m_CS[0].SetTexture(pressureBoundaryHandle, "_P", m_pressure);
            m_CS[0].Dispatch(pressureBoundaryHandle, 
                IRenderer.computeGroups(m_pressure.rt.width, 4), 
                IRenderer.computeGroups(m_pressure.rt.height, 4), 
                IRenderer.computeGroups(m_pressure.rt.volumeDepth, 4));
        }
            
        int projectHandle = m_CS[0].FindKernel("PROJECT_VELOCITY");
        m_CS[0].SetVector("_resV", new Vector4(m_velocity.rt.width, m_velocity.rt.height, m_velocity.rt.volumeDepth, 1));
        m_CS[0].SetTexture(projectHandle, "_V", m_velocity);
        m_CS[0].SetTexture(projectHandle, "_P_tex", m_pressure);
        m_CS[0].SetFloat("_dt", Time.deltaTime);
        m_CS[0].Dispatch(projectHandle, 
            IRenderer.computeGroups(m_pressure.rt.width, 4), 
            IRenderer.computeGroups(m_pressure.rt.height, 4), 
            IRenderer.computeGroups(m_pressure.rt.volumeDepth, 4));

        // Impose boundary conditions.
        int imposeBoundaryHandle = m_CS[0].FindKernel("IMPOSE_BOUNDARY");
        m_CS[0].SetVector("_resV", new Vector4(m_velocity.rt.width, m_velocity.rt.height, m_velocity.rt.volumeDepth, 1));
        m_CS[0].SetTexture(imposeBoundaryHandle, "_V", m_velocity);
        m_CS[0].Dispatch(imposeBoundaryHandle, 
            IRenderer.computeGroups(m_velocity.rt.width, 4), 
            IRenderer.computeGroups(m_velocity.rt.height, 4), 
            IRenderer.computeGroups(m_velocity.rt.volumeDepth, 4));
    }

    private void simulateDensity() {
        // Advection by velocity.
        Graphics.CopyTexture(m_density, m_densityTemp);
        int advectDensityHandle = m_CS[0].FindKernel("ADVECT_DENSITY");
        m_CS[0].SetVector("_resD", new Vector4(m_density.rt.width, m_density.rt.height, m_density.rt.volumeDepth, 1));
        m_CS[0].SetVector("_resV", new Vector4(m_velocity.rt.width, m_velocity.rt.height, m_velocity.rt.volumeDepth, 1));
        m_CS[0].SetTexture(advectDensityHandle, "_D", m_density);
        m_CS[0].SetTexture(advectDensityHandle, "_D_tex", m_densityTemp);
        m_CS[0].SetTexture(advectDensityHandle, "_V_tex", m_velocity);
        m_CS[0].SetFloat("_dt", Time.deltaTime);
        m_CS[0].Dispatch(advectDensityHandle, 
            IRenderer.computeGroups(m_density.rt.width, 4), 
            IRenderer.computeGroups(m_density.rt.height, 4), 
            IRenderer.computeGroups(m_density.rt.volumeDepth, 4));
        
        // Density source.
        int addDensityHandle = m_CS[0].FindKernel("ADD_DENSITY");
        m_CS[0].SetVector("_resD", new Vector4(m_density.rt.width, m_density.rt.height, m_density.rt.volumeDepth, 1));
        m_CS[0].SetVector("_resV", new Vector4(m_velocity.rt.width, m_velocity.rt.height, m_velocity.rt.volumeDepth, 1));
        m_CS[0].SetTexture(addDensityHandle, "_D", m_density);
        m_CS[0].SetFloat("_d", m_densitySource);
        m_CS[0].SetFloat("_dt", Time.deltaTime);
        if (m_densitySourceTex == null) {
            m_CS[0].SetTexture(addDensityHandle, "_DensitySource", Texture2D.whiteTexture);
        } else {
            m_CS[0].SetTexture(addDensityHandle, "_DensitySource", m_densitySourceTex);
        }
        m_CS[0].Dispatch(addDensityHandle, 
            IRenderer.computeGroups(m_density.rt.width, 4), 
            IRenderer.computeGroups(m_density.rt.height, 4), 
            1);

        // Impose boundary conditions.
        int imposeBoundaryHandle = m_CS[0].FindKernel("IMPOSE_BOUNDARY");
        m_CS[0].SetVector("_resV", new Vector4(m_density.rt.width, m_density.rt.height, m_density.rt.volumeDepth, 1));
        m_CS[0].SetTexture(imposeBoundaryHandle, "_V", m_density);
        m_CS[0].Dispatch(imposeBoundaryHandle, 
            IRenderer.computeGroups(m_density.rt.width, 4), 
            IRenderer.computeGroups(m_density.rt.height, 4), 
            IRenderer.computeGroups(m_density.rt.volumeDepth, 4));
    }

    private void setupRenderer() {
        if (m_renderer == null)
            return;

        // Only render the base noise
        m_renderer.m_coverageIntensity = 1;
        m_renderer.m_structureIntensity = 0;
        m_renderer.m_structureMultiply = 0;
        m_renderer.m_detailIntensity = 0;
        m_renderer.m_detailMultiply = 0;
        m_renderer.m_baseWarpIntensity = 0;
        m_renderer.m_detailWarpIntensity = 0;

        // Update transform
        m_renderer.m_curved = false;
        m_renderer.gameObject.transform.position = gameObject.transform.position;
        m_renderer.transform.localScale = gameObject.transform.localScale;

        // Set the texture to be our density field
        if (m_density != null)
            m_renderer.SetTexture(CloudDatatypes.CloudNoiseLayer.Base, m_density.rt, 1);
    }

    private void reallocate(Vector3Int resolution) 
    {
        deallocate();
        m_density = RTHandles.Alloc(resolution.x, resolution.y, resolution.z,
                                dimension: TextureDimension.Tex3D,
                                colorFormat: GraphicsFormat.R16_SFloat,
                                enableRandomWrite: true,
                                useMipMap: true,
                                autoGenerateMips: false,
                                name: "Turbulence: Density");
        m_densityTemp = RTHandles.Alloc(resolution.x, resolution.y, resolution.z,
                                dimension: TextureDimension.Tex3D,
                                colorFormat: GraphicsFormat.R16_SFloat,
                                enableRandomWrite: true,
                                useMipMap: true,
                                autoGenerateMips: false,
                                name: "Turbulence: Density Temp");
        m_velocity = RTHandles.Alloc(resolution.x, resolution.y, resolution.z,
                                dimension: TextureDimension.Tex3D,
                                colorFormat: GraphicsFormat.R16G16B16A16_SFloat,
                                enableRandomWrite: true,
                                useMipMap: true,
                                autoGenerateMips: false,
                                name: "Turbulence: Velocity");
        m_velocityTemp = RTHandles.Alloc(resolution.x, resolution.y, resolution.z,
                                dimension: TextureDimension.Tex3D,
                                colorFormat: GraphicsFormat.R16G16B16A16_SFloat,
                                enableRandomWrite: true,
                                useMipMap: true,
                                autoGenerateMips: false,
                                name: "Turbulence: Velocity Temp");
        m_pressure = RTHandles.Alloc(resolution.x, resolution.y, resolution.z,
                                dimension: TextureDimension.Tex3D,
                                colorFormat: GraphicsFormat.R16_SFloat,
                                enableRandomWrite: true,
                                useMipMap: true,
                                autoGenerateMips: false,
                                name: "Turbulence: Pressure");
        m_pressureTemp = RTHandles.Alloc(resolution.x, resolution.y, resolution.z,
                                dimension: TextureDimension.Tex3D,
                                colorFormat: GraphicsFormat.R16_SFloat,
                                enableRandomWrite: true,
                                useMipMap: true,
                                autoGenerateMips: false,
                                name: "Turbulence: Pressure Temp");
    }

    private void deallocate() 
    {
        if (m_density != null) {
            RTHandles.Release(m_density);
        }
        if (m_densityTemp != null) {
            RTHandles.Release(m_densityTemp);
        }
        if (m_velocity != null) {
            RTHandles.Release(m_velocity);
        }
        if (m_velocityTemp != null) {
            RTHandles.Release(m_velocityTemp);
        }
        if (m_pressure != null) {
            RTHandles.Release(m_pressure);
        }
        if (m_pressureTemp != null) {
            RTHandles.Release(m_pressureTemp);
        }
        m_density = null;
        m_densityTemp = null;
        m_velocity = null;
        m_velocityTemp = null;
        m_pressure = null;
        m_pressureTemp = null;
    }
}

} // namespace Turbulence
