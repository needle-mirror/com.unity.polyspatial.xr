# Porting existing Unity projects into PolySpatial XR
When porting an existing project into a new platform, it is important to know what potential technical risks and challenges you will face; PolySpatial XR is no exception. This section discusses several aspects of Unity projects, with emphasis on their support in PolySpatial XR.

It is worth mentioning that some Unity features are not supported so you will need to plan your project with this info at hand. 

## Input
The Input System allows users to control your game or app using a device, touch, or gestures. In projects developed for PolySpatial XR, the supported Input system is the [new Input system](https://docs.unity3d.com/Packages/com.unity.inputsystem@latest/index.html). This input system is intended to be a replacement for Unity's classic Input Manager.

Projects that use [Unity's classic input system](https://docs.unity3d.com/ScriptReference/Input.html) will not work and are required to be ported to use the new input system as mentioned above.

## Rendering

### Render Pipelines
By default Unity PolySpatial XR supports both [Universal Render Pipeline (URP)](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest/index.html) and [Unity's built in render pipeline](https://docs.unity3d.com/Manual/built-in-render-pipeline.html).

Unity PolySpatial XR doesn't support **custom ShaderLab shaders** on any render pipeline, and if your project uses custom shaders, all of them will have to be authored using [Unity's Shader graph](https://docs.unity3d.com/Packages/com.unity.shadergraph@latest/index.html). The ParticleSystem component only support materials with Unity's built-in shaders, Unity's Shader graph support is an on going work in progress for particles.

It is also important to norw that some components have certain limitations like limited light support, no direct control over shadows, no post processor support, and other limitations as follow:

| **Component**             | **Status**                                                                                                                                                                                                                 |
|---------------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| **MeshRenderer**          | &#8226; No support for "Lighting" (shadows, GI)<br/> &#8226; No support for "Probes"<br/>&#8226; No support for this component in Immediate mode <br/>&#8226; No support for "Additional Settings" (dynamic occlusion, rendering layer) |
| **Light**                 | &#8226; No Baking<br/>&#8226; No control over indirect lighting<br/>&#8226; No cookies<br/>&#8226; Will ignore shadow data<br/> &#8226; Will ignore render mode and culling mask                                                          |
| **SkinnedMeshRenderer**   | &#8226; Unoptimized animation only |
| **Halo**                  | Not supported |
| **Lens Flare**            | Not supported |
| **Line Rendering**        | Not supported |
| **Projector**             | Not supported |
| **Trail Renderer**        | Not supported |
| **Visual Effects**        | Not supported |
| **Probe Volumes**         | Not supported |
| **Camera**                | Not supported |
| **Lens Flare**            | Not supported |
| **Level of Detail (LoD)** | Not supported |
| **Occlusion Area**        | Not supported |
| **Occlusion Portal**      | Not supported |
| **Skybox**                | Not supported |
| **URP Decal projector**   | Not supported |
| **Tilemap Renderer**      | Not supported |
| **Video Player**          | Not supported |
| **Graphics Raycaster**    | Not supported |
| **Shaderlab Shaders**     | Not supported |
| **Post Processors**       | Not supported |
| **Lightmapping**          | Not supported |
| **Baked Lighting**        | Not supported |
| **Enlighten**             | Not supported |
| **Light Probes**          | Not supported |
| **Reflection Probes**     | Not supported |
| **Trees**                 | Not supported |
| **Fog**                   | Not supported |

It is worth noting that depending on your project needs, you will need to have workarounds for these limitations by either creating your own implementations or using other packages that are supported in the platform.

Cameras for example are not supported since the host platform is in charge of all the rendering and will not always expose camera info, so your project will have to adapt to these type of constraints.

### Particle systems
Unity particles in PolySpatial XR is an on going work in progress. In the table below you will find that a subset of modules / settings are partially supported in Unity's [Particle system](https://docs.unity3d.com/Manual/class-ParticleSystem.html) as follows:

| **Module**                       | **Status**          |
|----------------------------------|---------------------|
| **Emission**                     | Partially supported |
| **Shape**                        | Partially supported |
| **Velocity over lifetime**       | Partially supported |
| **Limit Velocity over lifetime** | Partially supported |
| **Inherit velocity**             | Partially supported |
| **Force over lifetime**          | Partially supported |
| **Color over lifetime**          | Partially supported |
| **Color by speed**               | Not Supported       |
| **Size over lifetime**           | Partially supported |
| **Size by speed**                | Not Supported       |
| **Rotation over lifetime**       | Partially supported |
| **Rotation by speed**            | Not Supported       |
| **External Forces**              | Not Supported       |
| **Noise**                        | Partially supported |
| **Collision**                    | Partially supported |
| **Triggers**                     | Not Supported       |
| **Sub Emitters**                 | Not Supported       |
| **Texture sheet animation**      | Partially supported |
| **Lights**                       | Partially supported |
| **Trails**                       | Not Supported       |
| **Custom Data**                  | Not Supported       |
| **Renderer**                     | Partially supported |

## User Interface (UI)
[Unity UI](https://docs.unity3d.com/Manual/UIToolkits.html) is expected to work but only on world space; there is no screen space support for it and advanced visual features like masking, shadowing, etc will not work for the time being. 
Below you will find a table of UI related components and their status:

| **Component**       | **Status**                                                                |
|---------------------|---------------------------------------------------------------------------|
| **TextMesh**        | Supported                                                       |
| **Canvas Renderer** | Partially Supported                                                       |
| **Sprite Renderer** | Supported                                                       |
| **TextMesh Pro**    | &#8226; Partially Supported<br/>&#8226; Raster only<br/> &#8226; No custom shaders |
| **Rect Transform**  | No specific support for sizing                                            |  

All in all, Canvases and all the UI layout components will not have screen space support and will offer partial world space support.

## Other Unity Components / Systems
It's impossible to cover all the component systems and packages that Unity exposes in this page but this section will try to give you an overview on what can be feasible to use from the rest of Unity systems.
Below you will find a list of supported and partially supported Unity components. 

| **Component**             | **Status**               |
|---------------------------|--------------------------|
| **Transform**             | Supported                |
| **Audio**                 | No spatial audio support |
| **MeshFilter**            | Supported                |
| **Animation / Animators** | Supported                |
| **2D Physics**            | Supported                |
| **3D Physics**            | Supported                |
| **Scripts**               | Supported                |
| **Terrain**               | Experimental support       |

`MonoBehaviours` are expected to work but they will depend on a case by case basis depending on which other components your scripts interact with.

# Final thoughts
Unity has many more components, but the main parts of the average XR app were covered in this section. Generally speaking, your existing Unity projects will likely require work to port to PolySpatial XR.

You will need to experiment, investigate, and adapt to the PolySpatial XR requirements and constraints by either writing your own PolySpatial XR-compatible systems or finding workarounds to these limitations to support your existing features.