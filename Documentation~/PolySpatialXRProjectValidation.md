# Unity PolySpatial XR Project Validation

This section covers several settings checks under your Unity project when developing for PolySpatial XR devices.

 The [Project validation](https://docs.unity3d.com/Packages/com.unity.xr.core-utils@latest?subfolder=/manual/project-validation.html) system helps you avoid common Scene and Project configuration issues you might encounter depending on the packages installed and the platform you are building for since not all Unity components are fully supported by Unity PolySpatial XR.

 Some features are partially supported and others are not supported at all. The Project validation system in PolySpatial XR helps you to detect and fix these unsupported features and configurations in your scenes. The system has a set of [Project Validation](https://docs.unity3d.com/Packages/com.unity.xr.core-utils@latest?subfolder=/manual/project-validation.html) rules to evaluate the components of your opened Scenes.

![Scene Validation display](images/SceneValidation/scene-validation.png)

## Hierarchy Issue icons (1)

When a component fails to pass one or more PolySpatial XR Scene Validation rules, the associated Game Object will have a yellow warning icon in the Hierarchy view and its root Game Object will have a greyed out warning icon. You can click in the greyed out warning icon to select the first child game object with an issue.

## Inspector issues header (2)

The Scene Validation system will automatically display an issue header in the Inspector of Game Objects that fail any validation rule. 

The issues header has buttons to navigate between Game Objects with issues or lets you open the Project Validation window to see all issues in your project and opened scenes. 

The **Fix this Object** button automatically fixes all validation issues in the currently selected Game Object. The **Hide/Show Alerts** button will hide or show the Component issues in all the Game Objects.

## Per Component Inspector issue (3)

The Scene Validation system automatically displays issue messages in the Inspector of Components that fail any Validation rules. 

The Component at hand that has one or more issues will contain a message explaining the issue. 

When pressing the **Fix** button, Unity will automatically fix the issue in the Component at hand; When pressing the **Edit** button, Unity will take you to the appropriate place in the Editor where you can correct the problem in your project. Both the **Fix** and **Edit** buttons provide tool tips that explain steps to manually correct the issue.
