---
uid: visionos-getting-started
---

# visionOS Beta Program Getting Started
## Table of Contents

* [Unity XR Overview](#unity-xr-overview)
* [visionOS Platform Overview](#visionos-platform-overview)
    * [Development Guide Overview](#development-guides)
    * [Requirements](#requirements)
    * [Known Limitations](#known-limitations)
    * [Supported Unity Features and Components](#supported-unity-features-and-components)
* [PolySpatial Overview](#polyspatial-overview)
    * [Modes and Volumes](#modes-and-volumes)
    * [Graphics](#graphics)
    * [Materials](#materials)
    * [Input](#input)
    * [PolySpatial Component Reference Guide](#polyspatial-component-reference-guide)
* [Immersive App Quick Start Guides](#immersive-app-quick-start-guides)
    * [Starting a new visionOS MR project from scratch](#starting-a-new-visionos-mr-project-from-scratch)
    * [Starting a new visionOS MR project from the Project Template](#starting-a-new-visionos-mr-project-from-the-project-template) 
    * [Sample Contents: Learn how to use visionOS with Examples](#sample-contents-learn-how-to-use-visionos-with-examples)
    * [Converting an existing Unity project](#converting-an-existing-unity-project)
        * [Converting a project from mobile to visionOS MR](#converting-a-project-from-mobile-to-visionos-mr)
        * [Converting a project from MR to visionOS MR](#converting-a-project-from-mr-to-visionos-mr)
* Best Practices
    * Developer Best Practices
    * Design Guidelines
* [Troubleshooting](#troubleshooting)
* [Glossary](#glossary)

# Unity XR Overview
<a name="unity-xr-overview"></a>

<!-- Describe how visionOS/PolySpatial fit into existing Unity XR tech stack, including the relationship/support for AR Foundation, XRI and relevant subsystems. We will have updated chart of this https://docs.unity3d.com/Manual/XRPluginArchitecture.html-->

# visionOS Platform Overview
<a name="visionos-platform-overview"></a>

Unity’s support for **visionOS** combines the full power of Unity's Editor and runtime engine with the rendering capabilities provided by **RealityKit**. Unity’s core features - including scripting, physics, animation blending, AI, scene management, and more - are supported without modification. This allows game and application logic to run on **visionOS **like any other Unity-supported platform, and the goal is to allow existing Unity games or applications to be able to bring over their logic without changes.

For rendering, **visionOS** support is limited to supporting features that **RealityKit** and Unity have in common. Common features such as meshes, materials, textures should work transparently. More complex features like particles are subject to limitations. Advanced features like full screen post processing and decals are currently unsupported, though this may change in the future. For more details, see "[Supported Unity Features & Components](#supported-unity-features-and-components)." 

Building for the visionOS platform using PolySpatial tech in Unity adds new functionality to support XR content creation that runs on separate devices, while also having a seamless and effective development experience. Most importantly, Unity PolySpatial for visionOS reacts to real-world and other AR content by default like any other XR Unity app.

## Development Guides
<a name="development-guides"></a>

* [visionOS Platform Configuration](DevelopingForPolySpatialXR.md) - Covers important sections when developing for visionOS: dependency packages, platform specific settings. 
    * [Unity Project conversion guide for PolySpatial and visionOS](#unity-project-conversion-guide-for-polyspatial-and-visionos) section provides information about porting your existing project over
    * [The Supported Unity Features and Components](#supported-unity-features-and-components) section provides information for your potential risks and project features failure.
* [visionOS Component Reference Guide](PolySpatialXRComponentReferenceGuide.md) - Overview of every component available when developing for visionOS.

The [FAQ](FAQ.md) presents answers to many common questions about design, implementation, and use of the visionOS package.

See [Glossary](Glossary.md) for definitions of common keywords to quickly understand the terminology used across this documentation.

## Requirements
<a name="requirements"></a>

### Unity Version

In order to develop for visionOS, Unity 2022.3 (LTS) is required.  **Versions of Unity before 2022.3 cannot be supported.**

A developer can get started without the PolySpatial beta with an existing project by bringing it up to 2022.3 before starting to work on a visionOS port.

### Graphics
<a name="graphics"></a>

Your project must use either the Universal Render Pipeline (URP) or the Builtin Render Pipeline (BRP). URP is preferred; if you are considering migrating your project, this would be a good opportunity to do so. Migration documentation is available for moving to URP from the legacy pipeline. ([Move on over to the Universal Render Pipeline with our advanced guide | Unity Blog](https://blog.unity.com/technology/move-on-over-to-the-universal-render-pipeline-with-our-advanced-guide))

#### Shaders & Materials
<a name="shaders-and-materials"></a>

PolySpatial for visionOS supports Shadergraph-authored custom materials.  

ShaderLab or other hand-coded shaders are not supported, as RealityKit doesn't currently expose a low-level shading language. Instead, several important standard shaders for each pipeline have been mapped to their closest available RealityKit analog. Current support includes:
* Standard URP shaders: Lit, Simple Lit, Unlit, (+TBD - more coming)
* Standard Builtin shaders: Standard, (+TBD – more coming)


#### Components and Features

See the “Supported Features & Components” at the end of this document for a detailed list of features and their status.

## Known Limitations
<a name="known-limitations"></a>

Currently Unity visionOS support is shipped as a bata product. Be mindful that since this is an early release, documentation, workflows, and especially API changes will occur, if you have any feedback, please contact us in the Unity Discussions space.


## Supported Unity Features & Components
<a name="supported-unity-features-and-components"></a>

Most Unity components that do not perform rendering or have other outputs should be supported. See also [Porting Unity Projects to PolySpatial XR](PortingUnityProjectsToPolySpatialXR.md)

* Transform
* Mesh Filter
* Mesh Renderer
    * No support for "Lighting" (shadows, GI).
    * No support for "Probes".
    * No support for "Additional Settings" (dynamic occlusion, rendering layer).
* SkinnedMeshRenderer
    * Certain skinned mesh skeletons may not be supported, such as models with import option “OptimizeGameObject” enabled.
* Animator
* 2D Physics (all)
    * Components like Rigidbody and Colliders are supported.
* 3D Physics (all)
    * Components like Rigidbody and Colliders are supported.
* Rect Transform
    * No specific support for sizing.
* Canvas and all "Layout Components"
    * Worldspace canvas partially supported.
    * No current screen-space support.
* TextMesh
    * Legacy TextMesh is not supported.
* TextMeshPro
    * Worldspace only.
* UGUI
    * Not all components are supported. Images and Buttons are supported, but other components may not be. 
    * Worldspace only; no screen space support.
    * Advanced visual features (masking, shadowing, etc) are not supported.
    * Legacy UI is not supported.
* SpriteRenderer
    * At this time, simple sprite masks work when Unity platform is the target, but not when targeting visionOS.
    * Sprite shapes, sprite tiles and packed sprites are not currently supported.
* Sorting Groups
    * Sorting Groups currently only work with sprites and not as a general rendering grouping/sorting device.
* Scripts
    * Most scripts utilizing the above listed features and components should work as expected.
* Particles
    * A subset of modules are supported. In general, curves are not supported for RealityKit. Gradients are supported, but have limitations. 
    * If using the gradients, only two alpha keys and two color keys are supported. 
    * Emission burst is not supported.
    * The only supported material currently is URP/Particles/Unlit.
* Shader Graph

# PolySpatial Overview
<a name="polyspatial-overview"></a>

<!-- insert a super amazing branding picture of PolySpatial -->

<!-- Paragraph to be replaced by product and marketing team’s polished phrasing -->

New mixed reality devices entering the market will enable multi-tasking experiences seamlessly integrated into users' homes. These devices open up a new world of possibilities around personal productivity, lifestyle and entertainment applications and a whole new market for developers. 

The PolySpatial package allows one to decouple different parts of an application so the core and rendering parts can easily be split to reduce the computational cost on the targeted device and focus its performance on a better rendering leaving the core application to be processed somewhere else.

Developers can think about the PolySpatial tech platform feature as a scene graph-oriented graphics API that is puppeteered by Unity running on a foreign device.


## Modes and Volumes
<a name="modes-and-volumes"></a>

Mixed reality content on visionOS can be in one of two modes, which we refer to as "shared" and "exclusive" mode.

| **Modes** | **Description** |
| --- | --- |
| Shared | In "shared" mode, your application coexists with any other applications that are active in the shared real-world space. Each application has one or more **bounded volumes** (see below), but no unbounded volumes. The position and orientation of these volumes (both relative and absolute) is opaque to the app. Input in this mode is limited to a “3D touch” mechanism, via the **PolySpatialTouchSpace** device (see Input below). In addition, ARKit information such as hand position, planes, or world mesh is unavailable in this mode. |
| Exclusive | In "exclusive" mode, a single application controls the entire view, via an **unbounded volume** (see below) in addition to previously created bounded volumes. In this mode, an app knows the relative positioning of its volumes, can access all AR features of the device, and use hand/joint position information to drive input and interactions directly. The app still does not have the ability to move or size bounded volumes, and thus must rely on the user to ensure bounded volumes don't overlap with meaningful content within the unbounded volume. |


### Volumes

Volumes are a new concept for mixed reality platforms. An application can create one or more volumes for displaying content in the mixed reality space. Each volume is an oriented box that contains 3D content. In visionOS, volumes can be moved and scaled in real-world space independently by the user, but not programmatically by the developer. Unity devs interact with Volumes using a new Unity component called a "Volume Camera" described below.

| **Modes** | **Description** |
| --- | --- |
| Bounded Volumes | Bounded volumes have a finite, box-shaped extent. Bounded volumes can be moved, resized and transformed in world space by the user, but not programmatically by the developer. Currently, Unity content within a bounded volume will expand to fill the actual size of the volume. Different options will be available in the future (for example, to see more of the scene instead of resizing it).<br><br>Input in bounded volumes is limited to “3D Touch” as provided by the PolySpatialTouchSpace device.  See Input below. |
| Unbounded Volumes | When running in exclusive mode, content presents a single unbounded volume, without any clipping edges. The application owns the entire mixed reality view, with no other applications visible. Additional bounded volumes from the same application can co-exist with this unbounded volume.<br><br>Within the unbounded volume, an application can request access to full hand tracking data. |


### Volume Camera

PolySpatial provides a new Unity component called a "Volume Camera" to interact with the modes and volumes provided by the visionOS environment. Volume cameras are similar to regular Unity cameras in that they indicate which content should be visible to the user, but differ in that they capture 3D content rather than a 2D image.

Add a VolumeCamera component to an object in a scene to specify how and what content is to be presented to the user.  Multiple VolumeCamera components in a scene are supported.

The transform of the GameObject that holds the VolumeCamera (e.g. scale) affects the size of the volume that is displayed to the user.  In-editor preview bounds for VolumeCamera can help visualize.

The **VolumeCamera** component exposes the following properties:

| **Mode** | Specifies the mode of the volume.
- **VolumeCameraMode.Unbounded**: The volume camera captures everything regardless of position, and the dimensions field is disabled and ignored. No more than one volume camera can be in unbounded mode at a given time for a given app. Setting the mode of a volume camera to Unbounded is equivalent to requesting your app switch to "exclusive" mode.
- **VolumeCameraMode.Bounded**: The volume camera has finite bounds as defined by dimensions. Any number of volume cameras can be in "bounded" mode, but switching an "unbounded" camera to "bounded" will exit "exclusive" mode and return the user to "shared" mode. |
| --- | --- |
| **Dimensions** | Defines the (unscaled) size of the camera's bounding box, with the box centered at the position of the **VolumeCamera**’s transform. The world space dimensions are calculated by element-wise multiplication of Dimensions and the transform's scale. |
| **CullingMask** | Defines a bitmask of Unity layers. Only objects belonging to the specified layers will be displayed by the volume camera. As for typical unity cameras and cullingMask workflows, this can be used to specify which object(s) are visible to each individual volume camera. For example, an inventory volume camera could be used to render a 3D inventory within one volume by defining an "inventory" layer, while a "minimap" layer might be used to render a bird's eye view of the entire scene within a second volume. |


## Graphics
<a name="graphics"></a>

On visionOS, Unity delegates all rendering to the platform so that the OS can provide the best performance, battery life, and rendering quality taking into account all currently running mixed reality applications. This imposes significant constraints on the graphics features that are available.

See “Graphics” in the [requirements section](#requirements) for specific constraints.

Rendering on RealityKit will most likely have visual differences over in Unity rendering. We are constantly working to improve visual equivalency between Unity and RealityKit but note there are differences.


## Material Swap Sets
<a name="materials"></a>

Because materials for RealityKit are restricted, you might want to continue using existing materials for non-visionOS platform builds, but use alternate materials for visionOS. Use the MaterialSwapSet asset to define this mapping. Create one or more MaterialSwapSet assets in your Resources folder. Within each one, a simple “from” and “to” material mapping is defined.  Material property overrides are not supported with this mechanism, though this might be possible in the future.

If you find this feature useful, please let us know on the Unity Discussions space.


## Input
<a name="input"></a>

There are two ways to capture user intent on visionOS: 3D touch and skeletal hand tracking. In exclusive mode, developers can also access head tracking data.


### 3D Touch and TouchSpace

In both bounded and unbounded volumes, a 3D touch input is provided when the user looks at an object with an input collider and performs the “pinch” (touch thumb and index finger together to “**tap**” or “**drag**”) gesture. The PolySpatialTouchSpace Input device provides that information to the developer. If the user holds the pinch gesture, a drag is initiated and the application is provided “move” updates relative to the original start point. Users may also perform the pinch gesture directly on an object if it is within arms reach (without specific gaze).

3D touch events are exposed via the **PolySpatialTouchSpace** Input device, which is built on top of the `com.unity.inputsystem` package, otherwise known as the New Input System. Existing actions bound to a touchscreen device should work for 2D input. For 3D input, users can bind actions to the specific **PolySpatialTouchSpace** device for a 3D position vector.

A collider with the collision mask set to the PolySpatial Input layer is required on any object that can receive 3D touch events.  Only touches against those events are reported.  At this time, the platform does not make available the gaze ray at the start of a tap gesture.


### Skeletal Hand Tracking

Skeletal hand tracking is provided by the **Hand Subsystem** in the **XR Hands Package**. Using a **Hand Visualizer** component in the scene, users can show a skinned mesh or per-joint geometry for the player’s hands, as well as physics objects for hand-based physics interactions. Users can write C# scripts against the **Hand Subsystem** directly to reason about the distance between bones and joint angles. The code for the **Hand Visualizer** component is available in the **XR Hands Package** and serves as a good jumping off point for code utilizing the **Hand Subsystem**.


### Head Tracking

Head tracking is provided by ARKit through the **ARKit Package**. This can be setup in a scene using the create menu for mobile AR. Create -> XR -> XR Origin (Mobile AR). The pose data comes through the new input system from devicePosition \[HandheldARInputDevice\] and deviceRotation \[HandheldARInputDevice\] 


# Quickstart Guides
<a name="#immersive-app-quick-start-guides"></a>

These guides have step by step instructions on how to get started building for visionOS.

* The [Immersive App Quick Start Guide](https://docs.google.com/document/d/1frBURb_Go1pVtDJ0_GWIdlVexVUel5bbiogkpq-Am9g/edit?usp=sharing) page is the ideal landing place for new users: it presents the workflow and information, it provides information about example projects and templates, and presents an overview of development best practices for the visionOS platform. 
* In the [How to Build Your visionOS App](#starting-a-new-visionos-mr-project-from-scratch) section, you will find a step-by-step tutorial that guides you through the complete workflow for installing, setting up and deploying a simple Unity app to an Apple Vision Pro device or visionOS Simulator.
* In the [Sample Contents: Learn how to use visionOS with Application Examples](#sample-con) section, you will find a wide range of vertical slices demo projects explaining how to develop for visionOS using PolySpatial tech. 


## Setting up your development environment

Prerequisites
* Hardware
* Software

Installing and setting up Unity
* Install Version
* Select Build Modules
* New Project


## Starting a new visionOS MR project from scratch
<a name="starting-a-new-visionos-mr-project-from-scratch"></a>

1. Select Edit > ProjectSettings…
2. Open the XR Plug-in Manager Menu
3. <!-- #{Placeholder insert pic about Platform SDK}# -->
4. Select File > Build Settings…
	1. Add Scenes (SampleScene)
	2. Select ‘Build’


## Starting a new visionOS MR project from the Immersive App Template
<a name="starting-a-new-visionos-mr-project-from-the-project-template"></a>

Unity’s [visionOS Template](https://docs.google.com/document/u/0/d/1frBURb_Go1pVtDJ0_GWIdlVexVUel5bbiogkpq-Am9g/edit) provides a starting point for visionOS development in Unity. The template configures project settings, pre-installs the right XR related packages, and includes various pre-configured Example Assets to demonstrate how to set up a project that is ready to deploy to visionOS.

The **visionOS Template** uses the following Unity features:

| **Package Name** | **Version #** | **Description** |
| --- | --- | --- |
| **visionOS Platform** | 1.0.0 |  |
| **PolySpatial** | 1.0.0 |  |
| **ARFoundation** | 5.0.0-pre.{latest} |  |
| **XR Interaction Toolkit** | 2.4.0-pre.{latest} | A high-level, component-based, interaction system for creating XR experiences. It provides a framework that makes 3D and UI interactions available from Unity input events. |
| **XR Hands** | 1.1.0-pre.{latest} |  |
| **XR Plugin Management** | 4.3.3 |  |
| **Input System** | 1.4.3 |  |
| **TextMeshPro** |  |  |
| **Universal RP** |  |  |

The visionOS Template demonstrates:

* Virtual Environment
* Mixed Reality Environment
* Shared World Environment 
* Multi-mode and Responsive Layout
* XR Input and Interactions


### Using the Template Quick Start

To use the visionOS project template, follow these steps:

* Install Unity 2022 LTS and make sure you add the right build targets for each platform you plan to deploy to. See the **Supported build targets** table on this page for more information.
* From the Unity Hub, click the dropdown next to New and create a new project in Unity 2022 LTS.
* Select the visionOS template and name your Project.
* Click Create.
* After your Project has been created, from Unity’s main menu, go to **Edit > Project Settings > XR Plug-in Management > visionOS SDK**, and select the platforms you plan to deploy to.
* Make sure **rendering** and quality settings are optimized for your target platform. See the **Rendering and quality settings** table on this page.


#### Removing the example Assets from the Scene

If you want to completely remove the example Assets from your Project, it is easy to do so.

1. In the Project window, open the **Assets** folder.
2. Right click the **visionOS Template Assets** folder.
3. Click **Delete**.
4. In the pop-up that appears, click **Delete**.

<!-- insert ref image -->


# Sample Contents: Learn how to use visionOS with Examples
<a name="sample-contents-learn-how-to-use-visionos-with-examples"></a>

### visionOS Samples

This project contains the majority of samples provided for visionOS; all samples within this project use the Universal Render Pipeline (URP). We are currently providing the following samples:


#### Samples/Tiny3D/1.Tiny3D.unity
A small, low-poly scene with just meshes using texture-free materials and lights.

#### Samples/DogPark/Scenes/DogPark.unity
Demonstrates a small scene with simple interactions and dynamics

#### Samples/MaterialTest/Scenes/ShaderTest.unity
Demonstrates a variety of materials, and highlights some of the current differences between Unity's URP and RealityKit. We expect the differences to converge over time.

#### Scenes/VerticalSliceDemo
This scene attempts to display all the major rendering features supported by visionOS and the dynamic modification of each. Exercised features include meshes, materials, textures, skinned mesh rendering, physics, dynamics, transform hierarchy changes, UI, text, sprites, and more.


# Converting an existing Unity project
<a name="converting-an-existing-unity-project"></a>

When porting existing Unity projects into visionOS, several considerations need to be taken into account. The biggest limitation is that some core Unity features aren't supported, and others provide a reduced feature set. In addition, input is different, and processing power and supported components will vary. Sometimes you will have to develop your own systems to support your unique project features and work around these limitations. You can find more information about porting your existing Unity project into visionOS in the [Porting Unity Projects To visionOS](PortingUnityProjectsToPolySpatialXR.md) page.


## Converting a project from mobile to visionOS MR
<a name="converting-a-project-from-mobile-to-visionos-mr"></a>

## Converting a project from MR to visionOS MR
<a name="converting-a-project-from-mr-to-visionos-mr"></a>

# Troubleshooting
<a name="troubleshooting"></a>

* **I enter play mode and see no visual or execution difference in my project!**
	* This probably indicates you haven't yet turned on support for the visionOS platform. To do so, go to Project Settings > PolySpatial > Enable PolySpatial Runtime.
* **The runtime is enabled, but nothing shows up!**
	* Ensure you have a Volume Camera in your scene.  If one is not present, currently a default one will be created that will include the bounds of every object in the scene, which might make those objects too small to see. An Unbounded camera with its origin positioned in the middle of your scene is a good starting point.
	* Verify that the in-editor preview runtime is functioning.  Open the “DontDestroyOnLoad” scene in the hierarchy while playing, and check if there is a PolySpatial Root” object present.  If there is not, ensure that the PolySpatial runtime is enabled.  If it is enabled and nothing shows up, please contact the Unity team.
	* When using an Unbounded camera, the platform is responsible for choosing the (0,0,0) origin.  It sometimes chooses a strange position for it.  Look around (literally) to see if your content is somewhere unexpected. Rebooting the device can also help to reset its session space, and it can be helpful to ensure that it is in a consistent location (for example, sitting on the desk, facing forward) every time you boot it up.
* **Skinned Meshes are not animating!**
	* On the animator component, ensure Culling Mode to Always Animate. 
	* If the model is imported, navigate to the Import Settings for the model. Under the Rig tab, ensure Optimize Game Object is unticked. Some models may not even have this setting, in that case, it should be good as-is.
	* Certain models may contain a skeleton (a set of bones in a hierarchy) that are incompatible with RealityKit. To be compatible, a skeleton must have the following attributes:
		1. A group of bones must have a common ancestor game object in the transform hierarchy. 
		2. Each bone in the skeleton must be able to traverse up the transform hierarchy without passing any non-bone game objects. 
	* In general, skeletons that have a non-bone game object somewhere in the skeleton (these are often used for scaling or offsets on bones) are not supported. 
* **I see an error on build about ScriptableSingleton**
	* This comes from the AR Foundation package and is benign. You can ignore this error.
* **I see a NULL ref or other issues in the log related to XXXX Tracker (Mesh Tracker, Sprite Tracker, etc)**
	* Locate the Runtime flags option in the PolySpatial settings and select the tracker that is causing issues. This will disable changes from those types of objects in PolySpatial. Please flag the issue with the team so we can understand and fix the tracker type.
* **My TextMeshPro text shows up as Pink glyph blocks or My Text mesh pro is blurry**
	* Make sure ‘Enable Shader Graph Text’ is checked in PolySpatial Settings
	* Locate the shader graphs included in the visionOS Package (visionOS/Resources/Shaders) and right click -> Reimport. 

# Glossary
<a name="glossary"></a>

| **PolySpatial** | |
| **visionOS** | |
| **RealityKit** | |
| **Shared Mode** | |
| **Immersive Mode** | |
| **Full Immersive Mode** | |