# Tutorial Framework Documentation

The purpose of this documentation is to provide additional details about the features of the Tutorial Framework.

## Editing narrative and instruction descriptions

You can add multiple paragraphs of text to the description fields of your tutorial page by adding line breaks between sections of text. IET parses this text and performs line wrapping per word by default, unless Chinese, Japanese or Korean characters are detected - in which case character-based wrapping is applied.

### Korean text

Due to "Han unification" - there is some overlap between Chinese, Japanese and Korean (later "CJK") characters / symbols. Korean uses spaces to distinguish between words and word wrapping is applied in a similar manner to English and most other languages.

For this purpose, we included a tag you can use to force word -based line wrapping. To force word-based wrapping, enclose the text with ```<wordwrap>```. After this, you should see wrapping occurring for each word instead of each character.

You can find more details below on how to use ```<wordwrap>```and other rich text tags.

### Using rich text in tutorial instructions and narratives

The system support any rich text tags supported by UI Toolkit. See [the Unity Manual page on the subject](https://docs.unity3d.com/Manual/UIE-supported-tags.html) 

## Tutorial media

You can choose to show image or video at the top of a tutorial page by choosing an appropriate **Header Media Type**. The specifications and recommended properties for the media are:
- tutorial page image/video will be displayed at 300 x 150 (width x height) in inspector but users can pop-out the media into a windows they can resize at will
- welcome dialog image: 700 x 200
- in your texture asset's Import Settings, make sure you have chosen **Editor GUI and Legacy GUI** as the **Texture Type**.
- for video, WebM VP8 is recommended ([read more](https://docs.unity3d.com/Manual/VideoSources-FileCompatibility.html))
- the type Video URL allows to use a web url as a video source. The url needs to point directly to the video file.

## Future Prefabs and Criteria 

A good way to review many of the concepts within this guide is to create a "Future Prefab Instance". This is useful for tutorials that ask the user to spawn a prefab in the scene, and then modify values of said prefab within the scene. However, since the prefab doesn't exist in the scene until it's initialized, it requires some work, as follows.

First, let's create a new scene and a new Tutorial with at least two **Narrative with Instructions** tutorial pages, and the scene, attached to it: 

![](images/index050.png)

And assign it to the list of tutorials in the Tutorials file. For this tutorial, you can use any script with any property. This example features a simple Cube Prefab with a Prefab script attached. The script only contains a public int called magicNumber.

![](images/index051.png)

Make sure the prefab isn't currently spawned in the scene (you can have multiples of the same prefab, but this example is easier to explain using only one). Then, go to your first tutorial page and set it up similarly to this: 

![](images/index052.png)

With the following masking settings:

![](images/index053.png)

The criterion used is Unity.InteractiveTutorials.InstantiatePrefabCriterion. For the Prefab Parent, drag in the Prefab from the Project window. Click the **Add** (**+**) button next to "Future Prefab Instances: List is Empty" twice. This will create two new slots: None (Object) 

![](images/index054.png)

Now select the prefab from the Project window.
With the Prefab instance open in the Inspector, click on the lock in the upper right to lock the Inspector and make sure it won't change focus when you select another Asset or GameObject.

Open another Inspector window, and select the asset representing the first Tutorial Page.
From the Project window, drag the instantiated prefab into this first empty Future Prefab Instance slot.
Then drag the **PrefabComponent** script from the Inspector to the second one and save the scene.

![](images/index055.png)

You should now see two new children object appear in the tutorial page in the Project window, the first one being the reference to the GameObject, the second being a reference to the Prefab script. 

The newly created children of the tutorial page are the Future Prefab Instance's references. The names depends on where they were created (Paragraph 1, Criterion 1, 1: Prefab (GameObject). Depending on which paragraph, how many criteria, or what the object is, these might change. Avoid changing the names, to ensure that they work correctly. You will need to use these object in the next tutorial page.  

In the second tutorial page, let's add another instruction paragraph. We want the user to modify the magicNumber property on our prefab, so we'll add a PropertyModificationCriterion as criteria. Within this criteria are a couple of options we need to modify.  

Firstly, the **Property Path** is the name of the property. In the current prefab, it's named "magicNumber". If the property is derived from a different script, you can still access it, "Otherscript.magicNumber", for instance (Tip: If you're ever unsure what the property path is - for internal properties for instance - you can set your Inspector to Debug mode and alt-left click on the name of the property).

![](images/index056.png)

Next, the **Target Value Mode** can either be set to **Target Value** or to **Different Than Initial**. This setting is fairly self-explanatory - if you want a user to set a property to a specific value, enter the desired value in the **Target Value** property. If you just want the user to change a property without anything specific in mind, set it to **Different Than Initial** and you can ignore the Target Value property below. Remember to set a Target Value if you're using Target Value in Target Value Mode. 

In combination with that, you must set the **Target Value Type**. This denotes what type of property the user is changing. This has to be set correctly so make sure the type matches the property. In case of our magicNumber this would be an int, so set Target Value Type to integer.  

Lastly and most importantly is the **Target**, where a reference needs to be set. Try to drag the component reference created earlier to the Target: 

![](images/index057.png)

It is worth noting that for this type of Criteria, the IET Framework relies on assigning the component as target rather than the GameObject. If you simply assigned the GameObject, the system would not be able to detect changes on the **PrefabComponent** component.

You can now close the second Inspector window we opened and unlock the original Inspector on the right.  

Make sure your scene is saved. If you're running the tutorial, you can click the **Run Startup Code** in the tutorial authoring toolbar to restart the tutorial. This resets the tutorial progress, displays the first page, and reloads the scene.  

If you go through the tutorial now, you'll be able to set the magicNumber to 50 and see that this results in successful completion of the task.  

To finish, let's set up masking for this second page so that it only lets the user edit the property. 

Enable Masking on the second Tutorial page. Set **Selector Type** to Editor Window, **Editor Window Type** to InspectorWindow, **Mask Type** to Fully Unmasked and **Mask Size Modifier** to No Modifications. We also need to add an "Unmasked Controls", so click the **Add** (**+**) button. 

Within the Unmasked Controls, set **Selector Mode** to Property. The next step is to correctly set up the **Target Type**.  

![](images/index058.png)

The Target Type within this Unmasked Controls lists every single script that exists within Unity and any packages in your project. Your custom-created scripts should be towards the top of this list, unless you have Cinemachine installed, in which case they'll be below those. Note that your scripts don't include any inheritance - so your PrefabScript will be listed as just that, as opposed to other scripts which are listed as UnityEngine.Something.  Select the PrefabScript from this list of Target Types. Lastly, set the **Property Path** to magicNumber.  

![](images/index059.png)

Restart the Tutorial - but make sure you don't save! Saving the scene would mean that the tutorial would start with a Prefab already existing in the Hierarchy and therefore in the scene.

You should be able to go through the tutorial now with everything masked correctly.  

Remember that it's a best practice to have these two instructions on the same page. Though we have masked everything out, there's always a chance something will go awry when instructions for selecting an object and modifying it are on two separate pages.   

## Criteria Descriptions 

Here's a list of all other criteria with brief descriptions of what they do.  

Remember to save the scene any time that you assign an object reference from the scene. This adds a hidden component ([SceneObjectGuid](xref:Unity.Tutorials.Core.SceneObjectGuid)) to the scene that holds the reference required for this framework to work correctly.

**ActiveToolCriterion** 

This criterion checks which tool the user currently has active - View, Move, Rotate, Scale, Rect, or Transform. You can use it to ensure that a user currently has the appropriate tool selected.

**ArbitraryCriterion** 

This is a special criterion which allows the tutorial author to specify any kind of logic by writing code in ScriptableObject or MonoBehaviour and assigning an instance of an object and a function that returns a boolean as a callback. It was added in 0.5.1.

**BuildStartedCriterion** 

This checks if the user has initiated a build.

**ComponentAddedCriterion** 

This criterion requires a user to add a specific component to a specific game object. You can assign a target game object normally, and pick a required component from a list of all available components (pre-existing in the engine, packages and custom ones) 

**EditorWindowCriterion** 

This criterion is useful for asking a user to open a specific window within the editor. You can select the window from the **Editor Window Type** list, and have the option to close the window if it's currently open thus forcing the user to open it themselves. 

**FrameSelectedCriterion** 

This ensures that the user Frame Selects a particular Game Object. This only requires an Object Reference.  

**InstantiatePrefabCriterion** 

As described above, you can check if a user has instantiated an object, and also create a Future Prefab Instance to use in other Tutorial Criteria. Future Prefab Instances can be created for components as well, so make sure you're creating the appropriate reference!

**MaterialPropertyModifiedCriterion** 

Assigns a Material in the Target Slot and sets Material Property Path to "_Color" to check whether a user has changed the color of a material.

**PackageInstalledCriterion** 

Checks whether a Unity Package Manager (UPM) package is installed in the project.

**PlayModeStateCriterion** 

Checks which Play mode the Editor is in. It can be toggled between Playing or Not Playing.

**PrefabInstanceCountCriterion** 

Checks how many prefabs exist within a scene. Can change **Comparison Mode** to decide how **Instance Count** works. You can set it to check if the amount of a certain prefab within a scene is at least "x" amount, exactly "x" amount or no more than "x" amount. The "x" amount is set in Instance Count and ranges between 0 and 100. 

<!-- Dev note: PreprocessBuildCriterion exists as a side effect of BuildStartedCriterion implementation but it's not meant to be used by the end user. -->

**PropertyModificationCriterion** 

Checks whether a Property within a GameObject has been modified. You need the exact property path (tip: putting the editor into Debug mode and alt-left clicking on a property name will give you the correct name).

Set the Target Value Mode to be either a Target Value or to be Different Than Initial. This lets you decide if you want the user to set the property to be a specific value or just something different than it was at the beginning.  

The Target Value field is where you enter the required target value, if necessary.  

Target Value Type has to be set appropriately (Integer, Decimal, Text, Boolean or Color) for this to work.

**RequiredSelectionCriterion** 

Checks to ensure the user has selected the correct GameObject/asset. This can be an object in the Hierarchy or an asset from the Project window. Especially helpful to appear ahead of ModifyProperty criteria, etc. 

**SceneAddedToBuildCriterion** 

This allows you to check whether a specific scene exists in the Project Build Settings. If the scene already exists in the Build Settings, the criteria will automatically be completed upon reaching it. Otherwise, the user has to drag the scene in as expected.

**SceneViewCameraMovedCriterion** 

Checks if the user has moved, zoomed or orbited around within the current scene view. 

<!-- Dev note: MockCriterion exists for the devs but it's not visible for the end user. -->

## Tutorial Styles

The Tutorial Styles asset can be created within the Project window by right-clicking and going to **Create** > **Tutorials** > **Tutorial Styles**. This asset allows you to modify everything visual about the walkthrough:

![](images/index062.png)

In order for any of these changes to take effect, this asset must be assigned within the Tutorial Project Settings asset. 

### Tutorial Style Sheets

Tutorial Style Sheets can be used to fully customize the style of the Tutorials window. Use **Create** > **Tutorials** > **Light Tutorial Style Sheet** and **Dark Tutorial Style Sheet** to create style sheets for customization and assign the style sheets as Light/Dark Theme Style Sheet of your Tutorial Styles. The dark/light theme is applied according to the user's Editor Theme preference.

## Tutorial Project Settings

Tutorial Project Settings is another asset that you can create within the Project window, by right-clicking and going to **Create** > **Tutorials** > **Tutorial Project Settings**. The settings in this asset are automatically applied to the entire project as soon as this asset is created.

![](images/index063.png)

The settings allow you to modify the following:

**Welcome Page**

If set, this page is shown in the welcome dialog when the project is started for the first time.

**Initial Scene**

If set, this scene will be loaded when the project is started for the first time.

**Initial Camera Settings**

If enabled, allows for tweaking of the position/rotation of the camera of the Initial Scene

**Restore Default Assets On Tutorial Reload** 

When enabled, a backup of your project's `Assets` folder is made into `Tutorial Defaults` folder when the tutorial project is started for the first time in non-authoring mode, 
meaning, the Tutorial Authoring Tools package is not present. Every time a tutorial is started, the backup is restored, meaning that any modifications to the original tutorial project assets will be lost.
With this option disabled (the default), changes made to assets will remain as usual.

Enabling this feature means that the size of your project will double and you should configure your version control software to ignore the `Tutorial Defaults` folder.
 If your project has lot of large assets or includes several tutorials that build upon each other, you should have this option disabled to prevent errors!


**Tutorial Style**

This is where you assign your Tutorial Styles asset. This changes the visuals of your tutorials (see section above).

[IMGUI]: https://docs.unity3d.com/Manual/GUIScriptingGuide.html
[UI Toolkit]: https://docs.unity3d.com/Manual/UIElements.html