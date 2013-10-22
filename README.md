U3DXT
=====

Source code to the high-level API and examples used in the iOS SDK plugin from U3DXT for Unity3D. This plugin lets you access all the iOS SDK APIs using C#, Javascript, or Boo from within your Unity3D game.

To execute the code here, you will need <a href="http://www.unity3d.com">Unity3D</a> and <a href="http://u3d.as/content/vitapoly-inc/i-os-sdk-native-api-access-from-c-javascript-and-boo/50g">iOS SDK Plugin</a> from <a href="http://u3dxt.com">U3DXT</a>.


High-level Source Code
----------------------
U3DXT comes with the high-level API precompiled as DLL files. However, you can instead use the source code from here, so you can make changes to the high-level API.

Follow the steps below:

1.  Go to Assets/U3DXT/Plugins/, and delete the _HL files for the module that you want to use source.  For example, if you want to use source code for the Social module, you would delete U3DXTSocial_HL.dll and U3DXTSocial_HL.xml.  DON'T delete U3DXTSocial.dll because that is the low-level API.

2.  Copy the source code to the same Plugins folder.  For the Social module for example, copy the src/highlevel/social into the Plugins folder.

3.  When you upgrade to a new version of U3DXT, be sure to delete the same _HL files again and update source from GitHub.
