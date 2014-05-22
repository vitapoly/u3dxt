using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using U3DXT.Core;
using U3DXT.iOS.Native.Foundation;
using U3DXT.iOS.Native.UIKit;
using U3DXT.Utils;
using U3DXT.iOS.Native.Internals;
using System.IO;
using U3DXT.iOS.CoreImage;
using U3DXT.iOS.Native.CoreImage;
using U3DXT.iOS.Native.CoreGraphics;
using U3DXT.iOS.Native.OpenGLES;

namespace U3DXT.iOS.CoreImage {

	/// <summary>
	/// The ImageFilter class is used to apply filters to images.
	/// <p></p>
	/// iOS 7.0 comes with 23 new built-in filters with a total of 117.
	/// For more information about these built-in filters, their usage and availability, please see:
	/// <a href="https://developer.apple.com/library/ios/documentation/graphicsimaging/Reference/CoreImageFilterReference/Reference/reference.html">Apple Core Image Filter Reference</a>.
	/// <p></p>
	/// To use filters:
	/// <ol>
	/// <li>Create an instance of ImageFilter.</li>
	/// <li>Specify the input by calling SetInput() method.</li>
	/// <li>Call one of the filters or multiple at a time. Every filter you call uses the results of the last filter.
	/// The following example applies sepia, bloom, and color invert in that order:
	/// <pre>imageFilter.SepiaTone(1.0f);
	/// imageFilter.bloom(10.0f, 1.0f).colorInvert(); // chain them up</pre>
	/// </li>
	/// <li>Render the final image by calling Render().</li>
	/// </ol>
	/// 
	/// <p></p>
	/// All filter methods return the ImageFilter object itself, so you can easily chain them up.
	/// <p></p>
	/// All built-in filters are implemented as direct methods in this class.
	/// However, you can also access them by calling filter() method by supplying the name and arguments. See the Apple Core Image Filter Reference (linked above)
	/// for filters, their names, description, usage, and availability.  Some filters are only available on OS X.
	/// <p></p>
	/// You can also call AutoAdjust() to do auto enhancement and red-eye reduction.
	/// <p></p>
	/// <strong>Note on Performance:</strong> 
	/// The filtering work is done in the Render() method using the GPU. The filter methods are to set up and chain the filters.
	/// So, the Render() call takes the longest time. Despite using the GPU, depending on the filters set up, size of input image,
	/// and device, filtering on live video from camera or video stream may be slow.
	/// <p></p>
	/// The Render() method also blocks until the native API returns.
	/// </summary>
	public class ImageFilter {
		
		private CIContext _ciContext;
		
		private Dictionary<string,CIFilter> _filters = new Dictionary<string,CIFilter>();
		private CIImage _image = null;
		
		/// <summary>
		/// Constructs an instance of ImageFilter.
		/// Newer iOS devices and versions can use GPU to render images and is much faster than using CPU.
		/// However, some older devices and/or iOS versions (specifically iPad 1 on iOS 5.x)
		/// may not be able to use GPU and needs to use the CPU.  In that case, set the useGPU flag to false.
		/// </summary>
		/// <param name='useGPU'>
		/// useGPU true indicates to use GPU to render images; false indicates to use CPU.
		/// </param>
		public ImageFilter(bool useGPU = true)
		{
			if (useGPU) 
			{
				EAGLContext eaglContext = new EAGLContext(EAGLRenderingAPI.S2);
				Dictionary<object, object> opts = new Dictionary<object, object>();
				opts[CIContext.kCIContextWorkingColorSpace] = NSNull.Null();

				_ciContext = CIContext.Context(eaglContext, opts);
			} 
			else 
			{			
				_ciContext = CIContext.Context((Dictionary<object, object>) null);
			}
		}

		/// <summary>
		/// Sets the input image from a Texture2D object.
		/// </summary>
		/// <param name='input'>
		/// input the input image.
		/// </param>
		public void SetInput(Texture2D input) {
			_image = CIImage.FromTexture2D(input);
		}
		
		/// <summary>
		/// Sets the input image from a CIImage object.
		/// </summary>
		/// <param name='input'>
		/// input the input image.
		/// </param>
		public void SetInput(CIImage input) {
			_image = input;
		}

		/// <summary>
		/// Sets the input image from an UIImage object.
		/// </summary>
		/// <param name='input'>
		/// input the input image.
		/// </param>
		public void SetInput(UIImage input) {
			_image = new CIImage(input);
		}
		
		/// <summary>
		/// Renders the input image by applying previously set up filters, with the specified rectangle.
		/// If the output Texture2D object is specified, the rendered image is copied to the output object.
		/// Otherwise, it creates a new BitmapData object and the caller is responsible for disposing it after use (calling dispose()).
		/// 
		/// After the render, the input and previously set up filters are removed.
		/// </summary>
		/// <param name='rect'>
		/// the rectangle area on the final image to render.
		/// </param> 
		/// <return>
		///  the rendered image in a Texture2D object; if output is null, a new Texture2D is created, otherwise output is returned.
		/// </return>
		/// <param name='output'>
		/// the Texture2D to copy the render to.
		/// </param>
		/// <param name="scale">
		/// the scale.
		/// </param>
		/// <param name="rotateAngle">
		/// the angle to rotate the image in degrees. Valid values are multiples of 90.
		/// </param>
		public Texture2D Render(Rect rect, Texture2D output = null, float scale = 1.0f, float rotateAngle = 0f) {
			if (rotateAngle % 90f != 0f)
				return null;

			if (rotateAngle % 180f != 0f) {
				rect.Set(rect.x, rect.y, rect.height, rect.width);
			}

			CGImage cgimage = RenderToCGImage(rect);

			if (cgimage == null)
				return null;
			
			if (output)
				cgimage.CopyToTexture2D(output, scale, rotateAngle);
			else
				output = cgimage.ToTexture2D(scale, rotateAngle);

			cgimage = null;
			return output;
		}

		/// <summary>
		/// Renders to CGImage.
		/// </summary>
		/// <returns>The CGImage.</returns>
		/// <param name="rect">Rect.</param>
		public CGImage RenderToCGImage(Rect rect) {
			if (_image == null)
				return null;

			CGImage cgimage = _ciContext.CreateCGImage(_image, rect);
			_image = null;

			return cgimage;
		}
		
		/// <summary>
		/// Returns a filter with that name or null if it cannot be found.
		/// You can use the returned filter to find out what attributes a filter has such as inputs and their ranges.
		/// But you should not change those values directly.  Instead, use either filter() or one of the filter methods.
		/// </summary>
		/// <returns>
		/// a CIFilter object with the same name, or null if not found
		/// </returns>
		/// <param name='filterName'>
		/// the name of the filter; they all start with "CI".
		/// </param>
		public CIFilter GetFilter(string filterName){
		
			CIFilter aFilter = null;
			
			if (!_filters.TryGetValue(filterName, out aFilter)) {
				aFilter = CIFilter.Filter(filterName);
				_filters[filterName] = aFilter;
			}
			
			return aFilter;
		}
		
		/// <summary>
		/// Applies a filter directly by name and specified parameters.
		/// See the Apple Core Image Filter Reference (linked above) for filters, their names and usage.
		/// The "inputImage" parameter is automatically set to either the input image or the result of the last filter; so you must omit this parameter.
		/// </summary>
		/// <param name='filterName'>
		/// the name of the filter; they all start with "CI".
		/// </param>
		/// <param name='param'>
		/// an Object with keys and values as inputs to the filter; they all start with "input".
		/// </param>
		public ImageFilter Filter(string filterName, Dictionary<string,object> param){
			CIFilter aFilter = GetFilter(filterName);
			if (aFilter == null)
				throw new U3DXTException("Filter with name " + filterName + " not found.");
			
			// use last image as input
			if (_image != null)
			{
				aFilter.SetValueForKey(_image, CIFilter.kCIInputImageKey);
			}
			
			foreach(var pair in param)
			{
				aFilter.SetValueForKey(pair.Value, pair.Key);
			}

			_image = aFilter.ValueForKey(CIFilter.kCIOutputImageKey) as CIImage;
			return this;
		}	
		

		/// <summary>
		/// Applies auto-adjust filters to the image. This method analyzes the image, determines what
		/// filters it needs, enhanced values for their parameters, and applies them.
		/// 
		/// The imageOrientation argument is to detect faces in the image so it can apply some filters only to the faces.
		/// See constants in FaceDetector for a list of values.
		/// </summary>
		/// <returns>
		/// object itself, for chaining filters.
		/// </returns>
		/// <param name='imageOrientation'>
		/// the orientation of the image; default is FaceDetector.IMAGE_ORIENTATION_DEFAULT.
		/// </param>
		/// <param name='autoEnhance'>
		/// whether to auto enhance the image.
		/// </param>
		/// <param name='autoRedEye'>
		/// whether to apply red-eye reduction.
		/// </param>
		/// <exception cref='U3DXTException'>
		/// Is thrown when the error.
		/// </exception>
		public ImageFilter AutoAdjust(int imageOrientation = 1, Boolean autoEnhance = true, Boolean autoRedEye = true){
			if (_image == null)
				throw new U3DXTException("Must call setInput() first.");

			Dictionary<object,object> options = new Dictionary<object,object>();

			options["CIDetectorImageOrientation"] = imageOrientation;
			options[CIImage.kCIImageAutoAdjustEnhance] = autoEnhance;
			options[CIImage.kCIImageAutoAdjustRedEye] = autoRedEye;
			
			Array adjustments = _image.AutoAdjustmentFilters(options);
			
			foreach (CIFilter aFilter in adjustments) {
				aFilter.SetValueForKey(_image, CIFilter.kCIInputImageKey);
				_image = aFilter.ValueForKey(CIFilter.kCIOutputImageKey) as CIImage;
			}
				
			return this;
		}

		
		//----------------------------------------------------------------------------------------------------------

		/// <summary>Adds color components to achieve a brightening effect.</summary>
		/// <remarks>
		/// <p></p><b>CIAdditionCompositing</b>
		/// <p class="abstract">Adds color components to achieve a brightening effect.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>backgroundImage</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>Texture2D</c>
		///         object whose display name is Background Image.
		///       </p>
		///     
		///   
		/// 
		/// 
		///   <p></p><b>Discussion</b>
		///   <p>
		///     This filter is typically used to add highlights and lens flare effects. The formula used to create this filter is described in  Thomas Porter and Tom Duff. 1984.
		///     <span class="content_text">
		///       <a href="http://keithp.com/~keithp/porterduff/p253-porter.pdf" class="urlLink" rel="external">Compositing Digital Images</a>
		///     </span>
		///     .
		///     <em>Computer Graphics</em>
		///     , 18 (3): 253-259.
		///   </p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryHighDynamicRange</code>
		/// ,
		/// <code>CICategoryNonSquarePixels</code>
		/// ,
		/// <code>CICategoryInterlaced</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryCompositeOperation</code>
		/// <p></p><b>Localized Display Name</b>
		/// Addition
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='backgroundImage'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter AdditionCompositing(Texture2D backgroundImage) {
			return Filter("CIAdditionCompositing", new Dictionary<string, object>() {
				{"inputBackgroundImage",CIImage.FromTexture2D(backgroundImage)}
			});
		}

		/// <summary>Performs an affine transform on a source image and then clamps the pixels at the edge of the transformed image, extending them outwards.</summary>
		/// <remarks>
		/// <p></p><b>CIAffineClamp</b>
		/// <p class="abstract">Performs an affine transform on a source image and then clamps the pixels at the edge of the transformed image, extending them outwards.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>transform</em>
		///     
		///     
		///       <p>
		///         On iOS, an
		///         <c>Matrix4x4</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeTransform</code>
		///         . You must pass the transform as
		///         <c>byte[]</c>
		///         using a statement similar to the following, where
		///         <code>xform</code>
		///         is an affine transform:
		///       </p>
		///       
		///         <table>
		///           <tr>
		///             <td scope="row">
		///               <pre>
		///                 [myFilter setValue:[NSValue valueWithBytes:&amp;xform
		///                 <span/>
		///               </pre>
		///             </td>
		///           </tr>
		///           <tr>
		///             <td scope="row">
		///               <pre>
		///                 objCType:@encode(CGAffineTransform)]
		///                 <span/>
		///               </pre>
		///             </td>
		///           </tr>
		///           <tr>
		///             <td scope="row">
		///               <pre>
		///                 forKey:@"inputTransform"];
		///                 <span/>
		///               </pre>
		///             </td>
		///           </tr>
		///         </table>
		///       
		///       <p>
		///         On OSX, an
		///         <code>NSAffineTransform</code>
		///         object whose attribute type is
		///         <code>CIAttributeTypeTransform</code>
		///         .
		///       </p>
		///     
		///   
		/// 
		/// 
		///   <p></p><b>Discussion</b>
		///   <p>This filter performs similarly to the CIAffineTransform filter except that it produces an image with infinite extent. You can use this filter when you need to blur an image but you want to avoid a soft, black fringe along the edges.</p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryTileEffect</code>
		/// <p></p><b>Localized Display Name</b>
		/// Affine Clamp
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='transform'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter AffineClamp(Matrix4x4 transform) {
			return Filter("CIAffineClamp", new Dictionary<string, object>() {
				{"inputTransform",NSValue.Value(transform)}
			});
		}

		/// <summary>Applies an affine transform to an image and then tiles the transformed image.</summary>
		/// <remarks>
		/// <p></p><b>CIAffineTile</b>
		/// <p class="abstract">Applies an affine transform to an image and then tiles the transformed image.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>transform</em>
		///     
		///     
		///       <p>
		///         On iOS, an
		///         <c>Matrix4x4</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeTransform</code>
		///         . You must pass the transform as
		///         <c>byte[]</c>
		///         using a statement similar to the following, where
		///         <code>xform</code>
		///         is an affine transform:
		///       </p>
		///       
		///         <table>
		///           <tr>
		///             <td scope="row">
		///               <pre>
		///                 [myFilter setValue:[NSValue valueWithBytes:&amp;xform
		///                 <span/>
		///               </pre>
		///             </td>
		///           </tr>
		///           <tr>
		///             <td scope="row">
		///               <pre>
		///                 objCType:@encode(CGAffineTransform)]
		///                 <span/>
		///               </pre>
		///             </td>
		///           </tr>
		///           <tr>
		///             <td scope="row">
		///               <pre>
		///                 forKey:@"inputTransform"];
		///                 <span/>
		///               </pre>
		///             </td>
		///           </tr>
		///         </table>
		///       
		///       <p>
		///         On OSX, an
		///         <code>NSAffineTransform</code>
		///         object whose attribute type is
		///         <code>CIAttributeTypeTransform</code>
		///         .
		///       </p>
		///     
		///   
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryTileEffect</code>
		/// <p></p><b>Localized Display Name</b>
		/// Affine Tile
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='transform'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter AffineTile(Matrix4x4 transform) {
			return Filter("CIAffineTile", new Dictionary<string, object>() {
				{"inputTransform",NSValue.Value(transform)}
			});
		}

		/// <summary>Applies an affine transform to an image.</summary>
		/// <remarks>
		/// <p></p><b>CIAffineTransform</b>
		/// <p class="abstract">Applies an affine transform to an image.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>transform</em>
		///     
		///     
		///       <p>
		///         On iOS, an
		///         <c>Matrix4x4</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeTransform</code>
		///         . You must pass the transform as
		///         <c>byte[]</c>
		///         using a statement similar to the following, where
		///         <code>xform</code>
		///         is an affine transform:
		///       </p>
		///       
		///         <table>
		///           <tr>
		///             <td scope="row">
		///               <pre>
		///                 [myFilter setValue:[NSValue valueWithBytes:&amp;xform
		///                 <span/>
		///               </pre>
		///             </td>
		///           </tr>
		///           <tr>
		///             <td scope="row">
		///               <pre>
		///                 objCType:@encode(CGAffineTransform)]
		///                 <span/>
		///               </pre>
		///             </td>
		///           </tr>
		///           <tr>
		///             <td scope="row">
		///               <pre>
		///                 forKey:@"inputTransform"];
		///                 <span/>
		///               </pre>
		///             </td>
		///           </tr>
		///         </table>
		///       
		///       <p>
		///         On OSX, an
		///         <code>NSAffineTransform</code>
		///         object whose attribute type is
		///         <code>CIAttributeTypeTransform</code>
		///         .
		///       </p>
		///     
		///   
		/// 
		/// 
		///   <p></p><b>Discussion</b>
		///   <p>You can scale, translate, or rotate the input image. You can also apply a combination of these operations.</p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryGeometryAdjustment</code>
		/// <p></p><b>Localized Display Name</b>
		/// Affine Transform
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 5.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='transform'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter AffineTransform(Matrix4x4 transform) {
			return Filter("CIAffineTransform", new Dictionary<string, object>() {
				{"inputTransform",NSValue.Value(transform)}
			});
		}

		/// <summary>Transitions from one image to another by passing a bar over the source image.</summary>
		/// <remarks>
		/// <p></p><b>CIBarsSwipeTransition</b>
		/// <p class="abstract">Transitions from one image to another by passing a bar over the source image.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>targetImage</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>Texture2D</c>
		///         object whose display name is Target Image.
		///       </p>
		///     
		///     
		///       <em>angle</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeAngle</code>
		///         and whose display name is Angle.
		///       </p>
		///       <p>Default value: 3.14</p>
		///     
		///     
		///       <em>width</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeDistance</code>
		///         and whose display name is Width.
		///       </p>
		///       <p>Default value: 30.00</p>
		///     
		///     
		///       <em>barOffset</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeScalar</code>
		///         and whose display name is Bar Offset.
		///       </p>
		///       <p>Default value: 10.00</p>
		///     
		///     
		///       <em>time</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeTime</code>
		///         and whose display name is Time.
		///       </p>
		///       <p>Default value: 0.00</p>
		///     
		///   
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryTransition</code>
		/// <p></p><b>Localized Display Name</b>
		/// CIBarsSwipeTransition
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.5 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='targetImage'></param>
		/// <param name='angle'></param>
		/// <param name='width'></param>
		/// <param name='barOffset'></param>
		/// <param name='time'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter BarsSwipeTransition(Texture2D targetImage, float angle, float width, float barOffset, float time) {
			return Filter("CIBarsSwipeTransition", new Dictionary<string, object>() {
				{"inputTargetImage",CIImage.FromTexture2D(targetImage)},
				{"inputAngle",angle},
				{"inputWidth",width},
				{"inputBarOffset",barOffset},
				{"inputTime",time}
			});
		}

		/// <summary>Uses alpha values from a mask to interpolate between an image and the background.</summary>
		/// <remarks>
		/// <p></p><b>CIBlendWithAlphaMask</b>
		/// <p class="abstract">Uses alpha values from a mask to interpolate between an image and the background.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>backgroundImage</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>Texture2D</c>
		///         object whose display name is Background Image.
		///       </p>
		///     
		///     
		///       <em>maskImage</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>Texture2D</c>
		///         object whose display name is Mask Image.
		///       </p>
		///     
		///   
		/// 
		/// 
		///   <p></p><b>Discussion</b>
		///   <p>When a mask alpha value is 0.0, the result is the background. When the mask alpha value is 1.0, the result is the image.</p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryStylize</code>
		/// <p></p><b>Localized Display Name</b>
		/// Blend With Alpha Mask
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.9 and later and in iOS 7.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='backgroundImage'></param>
		/// <param name='maskImage'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter BlendWithAlphaMask(Texture2D backgroundImage, Texture2D maskImage) {
			return Filter("CIBlendWithAlphaMask", new Dictionary<string, object>() {
				{"inputBackgroundImage",CIImage.FromTexture2D(backgroundImage)},
				{"inputMaskImage",CIImage.FromTexture2D(maskImage)}
			});
		}

		/// <summary>Uses values from a grayscale mask to interpolate between an image and the background.</summary>
		/// <remarks>
		/// <p></p><b>CIBlendWithMask</b>
		/// <p class="abstract">Uses values from a grayscale mask to interpolate between an image and the background.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>backgroundImage</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>Texture2D</c>
		///         object whose display name is Background Image.
		///       </p>
		///     
		///     
		///       <em>maskImage</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>Texture2D</c>
		///         object whose display name is Mask Image.
		///       </p>
		///     
		///   
		/// 
		/// 
		///   <p></p><b>Discussion</b>
		///   <p>When a mask value is 0.0, the result is the background. When the mask value is 1.0, the result is the image.</p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryStylize</code>
		/// <p></p><b>Localized Display Name</b>
		/// Blend With Mask
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='backgroundImage'></param>
		/// <param name='maskImage'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter BlendWithMask(Texture2D backgroundImage, Texture2D maskImage) {
			return Filter("CIBlendWithMask", new Dictionary<string, object>() {
				{"inputBackgroundImage",CIImage.FromTexture2D(backgroundImage)},
				{"inputMaskImage",CIImage.FromTexture2D(maskImage)}
			});
		}

		/// <summary>Softens edges and applies a pleasant glow to an image.</summary>
		/// <remarks>
		/// <p></p><b>CIBloom</b>
		/// <p class="abstract">Softens edges and applies a pleasant glow to an image.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>radius</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeDistance</code>
		///         and whose display name is Radius.
		///       </p>
		///       <p>Default value: 10.00</p>
		///     
		///     
		///       <em>intensity</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeScalar</code>
		///         and whose display name is Intensity.
		///       </p>
		///       <p>Default value: 1.00</p>
		///     
		///   
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryStylize</code>
		/// <p></p><b>Localized Display Name</b>
		/// Bloom
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='radius'></param>
		/// <param name='intensity'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter Bloom(float radius, float intensity) {
			return Filter("CIBloom", new Dictionary<string, object>() {
				{"inputRadius",radius},
				{"inputIntensity",intensity}
			});
		}

		/// <summary>Generates a checkerboard pattern.</summary>
		/// <remarks>
		/// <p></p><b>CICheckerboardGenerator</b>
		/// <p class="abstract">Generates a checkerboard pattern.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>color0</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>Color3</c>
		///         object whose display name is Color 1.
		///       </p>
		///     
		///     
		///       <em>color1</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>Color3</c>
		///         object whose display name is Color 2.
		///       </p>
		///     
		///     
		///       <em>width</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeDistance</code>
		///         and whose display name is Width.
		///       </p>
		///       <p>Default value: 80.00</p>
		///     
		///     
		///       <em>sharpness</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeScalar</code>
		///         and whose display name is Sharpness.
		///       </p>
		///       <p>Default value: 1.00</p>
		///     
		///   
		/// 
		/// 
		///   <p></p><b>Discussion</b>
		///   <p>You can specify the checkerboard size and colors, and the sharpness of the pattern.</p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryGenerator</code>
		/// <p></p><b>Localized Display Name</b>
		/// Checkerboard
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='color0'>A color in RGBA format</param>
		/// <param name='color1'>A color in RGBA format</param>
		/// <param name='width'></param>
		/// <param name='sharpness'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter CheckerboardGenerator(Color32 color0, Color32 color1, float width, float sharpness) {
			return Filter("CICheckerboardGenerator", new Dictionary<string, object>() {
				{"inputColor0",CIColor.FromColor32(color0)},
				{"inputColor1",CIColor.FromColor32(color1)},
				{"inputWidth",width},
				{"inputSharpness",sharpness}
			});
		}

		/// <summary>Distorts the pixels starting at the circumference of a circle and emanating outward.</summary>
		/// <remarks>
		/// <p></p><b>CICircleSplashDistortion</b>
		/// <p class="abstract">Distorts the pixels starting at the circumference of a circle and emanating outward.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>center</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypePosition</code>
		///         and whose display name is Center.
		///       </p>
		///       <p>Default value: [150 150]</p>
		///     
		///     
		///       <em>radius</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeDistance</code>
		///         and whose display name is Radius.
		///       </p>
		///       <p>Default value: 150.00</p>
		///     
		///   
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryDistortionEffect</code>
		/// <p></p><b>Localized Display Name</b>
		/// Circle Splash Distortion
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='center'>An array of floats representing a vector</param>
		/// <param name='radius'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter CircleSplashDistortion(float[] center, float radius) {
			return Filter("CICircleSplashDistortion", new Dictionary<string, object>() {
				{"inputCenter",new CIVector("[" + string.Join(" ", Array.ConvertAll(center, x => x.ToString())) + "]")},
				{"inputRadius",radius}
			});
		}

		/// <summary>Simulates a circular-shaped halftone screen.</summary>
		/// <remarks>
		/// <p></p><b>CICircularScreen</b>
		/// <p class="abstract">Simulates a circular-shaped halftone screen.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>center</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypePosition</code>
		///         and whose display name is Center.
		///       </p>
		///       <p>Default value: [150 150]</p>
		///     
		///     
		///       <em>width</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeDistance</code>
		///         and whose display name is Width.
		///       </p>
		///       <p>Default value: 6.00</p>
		///     
		///     
		///       <em>sharpness</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeScalar</code>
		///         and whose display name is Sharpness.
		///       </p>
		///       <p>Default value: 0.70</p>
		///     
		///   
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryHalftoneEffect</code>
		/// <p></p><b>Localized Display Name</b>
		/// Circular Screen
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='center'>An array of floats representing a vector</param>
		/// <param name='width'></param>
		/// <param name='sharpness'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter CircularScreen(float[] center, float width, float sharpness) {
			return Filter("CICircularScreen", new Dictionary<string, object>() {
				{"inputCenter",new CIVector("[" + string.Join(" ", Array.ConvertAll(center, x => x.ToString())) + "]")},
				{"inputWidth",width},
				{"inputSharpness",sharpness}
			});
		}

		/// <summary>Uses the luminance values of the background with the hue and saturation values of the source image.</summary>
		/// <remarks>
		/// <p></p><b>CIColorBlendMode</b>
		/// <p class="abstract">Uses the luminance values of the background with the hue and saturation values of the source image.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>backgroundImage</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>Texture2D</c>
		///         object whose display name is Background Image.
		///       </p>
		///     
		///   
		/// 
		/// 
		///   <p></p><b>Discussion</b>
		///   <p>
		///     This mode preserves the gray levels in the image. The formula used to create this filter is described in the PDF specification, which is available online from the Adobe Developer Center. See
		///     <span class="content_text">
		///       <a href="http://www.adobe.com/devnet/pdf/pdf_reference.html" class="urlLink" rel="external">PDF Reference and Adobe Extensions to the PDF Specification</a>
		///     </span>
		///     .
		///   </p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryNonSquarePixels</code>
		/// ,
		/// <code>CICategoryInterlaced</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryCompositeOperation</code>
		/// <p></p><b>Localized Display Name</b>
		/// Color Blend Mode
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='backgroundImage'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter ColorBlendMode(Texture2D backgroundImage) {
			return Filter("CIColorBlendMode", new Dictionary<string, object>() {
				{"inputBackgroundImage",CIImage.FromTexture2D(backgroundImage)}
			});
		}

		/// <summary>Darkens the background image samples to reflect the source image samples.</summary>
		/// <remarks>
		/// <p></p><b>CIColorBurnBlendMode</b>
		/// <p class="abstract">Darkens the background image samples to reflect the source image samples.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>backgroundImage</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>Texture2D</c>
		///         object whose display name is Background Image.
		///       </p>
		///     
		///   
		/// 
		/// 
		///   <p></p><b>Discussion</b>
		///   <p>
		///     Source image sample values that specify white do not produce a change. The formula used to create this filter is described in the PDF specification, which is available online from the Adobe Developer Center. See
		///     <span class="content_text">
		///       <a href="http://www.adobe.com/devnet/pdf/pdf_reference.html" class="urlLink" rel="external">PDF Reference and Adobe Extensions to the PDF Specification</a>
		///     </span>
		///     .
		///   </p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryNonSquarePixels</code>
		/// ,
		/// <code>CICategoryInterlaced</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryCompositeOperation</code>
		/// <p></p><b>Localized Display Name</b>
		/// Color Burn Blend Mode
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='backgroundImage'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter ColorBurnBlendMode(Texture2D backgroundImage) {
			return Filter("CIColorBurnBlendMode", new Dictionary<string, object>() {
				{"inputBackgroundImage",CIImage.FromTexture2D(backgroundImage)}
			});
		}

		/// <summary>Modifies color values to keep them within a specified range.</summary>
		/// <remarks>
		/// <p></p><b>CIColorClamp</b>
		/// <p class="abstract">Modifies color values to keep them within a specified range.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>minComponents</em>
		///     
		///     
		///       <p>
		///         RGBA values for the lower end of the range. A
		///         <c>float[]</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeRectangle</code>
		///         and whose display name is MinComponents.
		///       </p>
		///       <p>Default value: [0 0 0 0] Identity: [0 0 0 0]</p>
		///     
		///     
		///       <em>maxComponents</em>
		///     
		///     
		///       <p>
		///         RGBA values for the upper end of the range. A
		///         <c>float[]</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeRectangle</code>
		///         and whose display name is MaxComponents.
		///       </p>
		///       <p>Default value: [1 1 1 1] Identity: [1 1 1 1]</p>
		///     
		///   
		/// 
		/// 
		///   <p></p><b>Discussion</b>
		///   <p>
		///     At each pixel, color component values less than those in
		///     <em>inputMinComponents</em>
		///     will be increased to match those in
		///     <em>inputMinComponents</em>
		///     , and color component values greater than those in
		///     <em>inputMaxComponents</em>
		///     will be decreased to match those in
		///     <em>inputMaxComponents</em>
		///     .
		///   </p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryNonSquarePixels</code>
		/// ,
		/// <code>CICategoryInterlaced</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryColorAdjustment</code>
		/// <p></p><b>Localized Display Name</b>
		/// Color Clamp
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.9 and later and in iOS 7.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='minComponents'>An array of floats representing a vector</param>
		/// <param name='maxComponents'>An array of floats representing a vector</param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter ColorClamp(float[] minComponents, float[] maxComponents) {
			return Filter("CIColorClamp", new Dictionary<string, object>() {
				{"inputMinComponents",new CIVector("[" + string.Join(" ", Array.ConvertAll(minComponents, x => x.ToString())) + "]")},
				{"inputMaxComponents",new CIVector("[" + string.Join(" ", Array.ConvertAll(maxComponents, x => x.ToString())) + "]")}
			});
		}

		/// <summary>Adjusts saturation, brightness, and contrast values.</summary>
		/// <remarks>
		/// <p></p><b>CIColorControls</b>
		/// <p class="abstract">Adjusts saturation, brightness, and contrast values.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>saturation</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeScalar</code>
		///         and whose display name is Saturation.
		///       </p>
		///       <p>Default value: 1.00</p>
		///     
		///     
		///       <em>brightness</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeScalar</code>
		///         and whose display name is Brightness.
		///       </p>
		///     
		///     
		///       <em>contrast</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeScalar</code>
		///         and whose display name is Contrast.
		///       </p>
		///       <p>Default value: 1.00</p>
		///     
		///   
		/// 
		/// 
		///   <p></p><b>Discussion</b>
		///   <p>
		///     To calculate saturation, this filter linearly interpolates between a grayscale image (saturation =
		///     <code>0.0</code>
		///     ) and the original image (saturation =
		///     <code>1.0</code>
		///     ). The filter supports extrapolation: For values large than
		///     <code>1.0</code>
		///     , it increases saturation.
		///   </p>
		///   <p>To calculate contrast, this filter uses the following formula:</p>
		///   <p>
		///     <code>(color.rgb - vec3(0.5)) * contrast  +  vec3(0.5)</code>
		///   </p>
		///   <p>This filter calculates brightness by adding a bias value:</p>
		///   <p>
		///     <code>color.rgb + vec3(brightness)</code>
		///   </p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryNonSquarePixels</code>
		/// ,
		/// <code>CICategoryInterlaced</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryColorAdjustment</code>
		/// <p></p><b>Localized Display Name</b>
		/// Color Controls
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 5.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='saturation'></param>
		/// <param name='brightness'></param>
		/// <param name='contrast'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter ColorControls(float saturation, float brightness, float contrast) {
			return Filter("CIColorControls", new Dictionary<string, object>() {
				{"inputSaturation",saturation},
				{"inputBrightness",brightness},
				{"inputContrast",contrast}
			});
		}

		/// <summary>Modifies the pixel values in an image by applying a set of polynomial cross-products.</summary>
		/// <remarks>
		/// <p></p><b>CIColorCrossPolynomial</b>
		/// <p class="abstract">Modifies the pixel values in an image by applying a set of polynomial cross-products.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>redCoefficients</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose display name is RedCoefficients.
		///       </p>
		///       <p>Default value: [1 0 0 0 0 0 0 0 0 0] Identity: [1 0 0 0 0 0 0 0 0 0]</p>
		///     
		///     
		///       <em>greenCoefficients</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose display name is GreenCoefficients.
		///       </p>
		///       <p>Default value: [0 1 0 0 0 0 0 0 0 0] Identity: [0 1 0 0 0 0 0 0 0 0]</p>
		///     
		///     
		///       <em>blueCoefficients</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose display name is BlueCoefficients.
		///       </p>
		///       <p>Default value: [0 0 1 0 0 0 0 0 0 0] Identity: [0 0 1 0 0 0 0 0 0 0]</p>
		///     
		///   
		/// 
		/// 
		///   <p></p><b>Discussion</b>
		///   <p>
		///     Each component in an output pixel
		///     <code>out</code>
		///     is determined using the component values in the input pixel
		///     <code>in</code>
		///     according to a polynomial cross product with the input coefficients. That is, the red component of the output pixel is calculated using the
		///     <em>inputRedCoefficients</em>
		///     parameter (abbreviated
		///     <code>rC</code>
		///     below) using the following formula:
		///   </p>
		///   
		///     <table>
		///       <tr>
		///         <td scope="row">
		///           <pre>
		///             out.r =        in.r * rC[0] +        in.g * rC[1] +        in.b * rC[2]
		///             <span/>
		///           </pre>
		///         </td>
		///       </tr>
		///       <tr>
		///         <td scope="row">
		///           <pre>
		///             + in.r * in.r * rC[3] + in.g * in.g * rC[4] + in.b * in.b * rC[5]
		///             <span/>
		///           </pre>
		///         </td>
		///       </tr>
		///       <tr>
		///         <td scope="row">
		///           <pre>
		///             + in.r * in.g * rC[6] + in.g * in.b * rC[7] + in.b * in.r * rC[8]
		///             <span/>
		///           </pre>
		///         </td>
		///       </tr>
		///       <tr>
		///         <td scope="row">
		///           <pre>
		///             + rC[9]
		///             <span/>
		///           </pre>
		///         </td>
		///       </tr>
		///     </table>
		///   
		///   <p>Then, the formula is repeated to calculate the blue and green components of the output pixel using the blue and green coefficients, respectively.</p>
		///   <p>This filter can be used for advanced color space and tone mapping conversions, such as imitating the color reproduction of vintage photography film.</p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryNonSquarePixels</code>
		/// ,
		/// <code>CICategoryInterlaced</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryColorEffect</code>
		/// <p></p><b>Localized Display Name</b>
		/// Color Cross Polynomial
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.9 and later and in iOS 7.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='redCoefficients'>An array of floats representing a vector</param>
		/// <param name='greenCoefficients'>An array of floats representing a vector</param>
		/// <param name='blueCoefficients'>An array of floats representing a vector</param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter ColorCrossPolynomial(float[] redCoefficients, float[] greenCoefficients, float[] blueCoefficients) {
			return Filter("CIColorCrossPolynomial", new Dictionary<string, object>() {
				{"inputRedCoefficients",new CIVector("[" + string.Join(" ", Array.ConvertAll(redCoefficients, x => x.ToString())) + "]")},
				{"inputGreenCoefficients",new CIVector("[" + string.Join(" ", Array.ConvertAll(greenCoefficients, x => x.ToString())) + "]")},
				{"inputBlueCoefficients",new CIVector("[" + string.Join(" ", Array.ConvertAll(blueCoefficients, x => x.ToString())) + "]")}
			});
		}

		/// <summary>Uses a three-dimensional color table to transform the source image pixels.</summary>
		/// <remarks>
		/// <p></p><b>CIColorCube</b>
		/// <p class="abstract">Uses a three-dimensional color table to transform the source image pixels.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>cubeDimension</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeCount</code>
		///         and whose display name is Cube Dimension.
		///       </p>
		///       <p>Default value: 2.00</p>
		///     
		///     
		///       <em>cubeData</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>byte[]</c>
		///         object whose display name is Cube Data.
		///       </p>
		///     
		///   
		/// 
		/// 
		///   <p></p><b>Discussion</b>
		///   <p>This filter applies a mapping from RGB space to new color values that are defined in inputCubeData.  For each RGBA pixel in inputImage the filter uses the R,G and B values to index into a thee dimensional texture represented by inputCubeData.  inputCubeData contains floating point RGBA cells that contain linear premultiplied values.  The data is organized into inputCubeDimension number of xy planes, with each plane of size inputCubeDimension by inputCubeDimension.  Input pixel components R and G are used to index the data in x and y respectively, and B is used to index in z.  In inputCubeData the R component varies fastest, followed by G, then B.</p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryNonSquarePixels</code>
		/// ,
		/// <code>CICategoryInterlaced</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryColorEffect</code>
		/// <p></p><b>Localized Display Name</b>
		/// Color Cube
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='cubeDimension'></param>
		/// <param name='cubeData'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter ColorCube(float cubeDimension, byte[] cubeData) {
			return Filter("CIColorCube", new Dictionary<string, object>() {
				{"inputCubeDimension",cubeDimension},
				{"inputCubeData",NSData.FromByteArray(cubeData)}
			});
		}

		/// <summary>Uses a three-dimensional color table to transform the source image pixels and maps the result to a specified color space.</summary>
		/// <remarks>
		/// <p></p><b>CIColorCubeWithColorSpace</b>
		/// <p class="abstract">Uses a three-dimensional color table to transform the source image pixels and maps the result to a specified color space.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>cubeDimension</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeCount</code>
		///         and whose display name is Cube Dimension.
		///       </p>
		///       <p>Default value: 2.00 Minimum: 2.00 Maximum: 128.00 Identity: 2.00</p>
		///     
		///     
		///       <em>cubeData</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>byte[]</c>
		///         object whose display name is Cube Data.
		///       </p>
		///     
		///     
		///       <em>colorSpace</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>CGColorSpace</c>
		///         object whose display name is ColorSpace.
		///       </p>
		///     
		///   
		/// 
		/// 
		///   <p></p><b>Discussion</b>
		///   <p>
		///     See
		///     <code>
		///       <a href="#//apple_ref/doc/filter/ci/CIColorCube">CIColorCube</a>
		///     </code>
		///     for more details on the color cube operation. To provide a
		///     <code>
		///       <a href="../../CGColorSpace/Reference/reference.html#//apple_ref/c/tdef/CGColorSpaceRef" target="_self">CGColorSpaceRef</a>
		///     </code>
		///     object as the input parameter, cast it to type
		///     <code>id</code>
		///     . With the default color space (null), which is equivalent to
		///     <code>kCGColorSpaceGenericRGBLinear</code>
		///     , this filter’s effect is identical to that of
		///     <code>CIColorCube</code>
		///     .
		///   </p>
		///   <p>
		///     <span class="content_text">Figure 23</span>
		///     uses the same color cube as
		///     <span class="content_text">
		///       <a href="#//apple_ref/doc/uid/TP30000136-SW56">Figure 22</a>
		///     </span>
		///     , but with the sRGB color space.
		///   </p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryNonSquarePixels</code>
		/// ,
		/// <code>CICategoryInterlaced</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryColorEffect</code>
		/// <p></p><b>Localized Display Name</b>
		/// Color Cube with ColorSpace
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.9 and later and in iOS 7.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='cubeDimension'></param>
		/// <param name='cubeData'></param>
		/// <param name='colorSpace'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter ColorCubeWithColorSpace(float cubeDimension, byte[] cubeData, CGColorSpace colorSpace) {
			return Filter("CIColorCubeWithColorSpace", new Dictionary<string, object>() {
				{"inputCubeDimension",cubeDimension},
				{"inputCubeData",NSData.FromByteArray(cubeData)},
				{"inputColorSpace",colorSpace}
			});
		}

		/// <summary>Brightens the background image samples to reflect the source image samples.</summary>
		/// <remarks>
		/// <p></p><b>CIColorDodgeBlendMode</b>
		/// <p class="abstract">Brightens the background image samples to reflect the source image samples.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>backgroundImage</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>Texture2D</c>
		///         object whose display name is Background Image.
		///       </p>
		///     
		///   
		/// 
		/// 
		///   <p></p><b>Discussion</b>
		///   <p>
		///     Source image sample values that specify black do not produce a change. The formula used to create this filter is described in the PDF specification, which is available online from the Adobe Developer Center. See
		///     <span class="content_text">
		///       <a href="http://www.adobe.com/devnet/pdf/pdf_reference.html" class="urlLink" rel="external">PDF Reference and Adobe Extensions to the PDF Specification</a>
		///     </span>
		///     .
		///   </p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryNonSquarePixels</code>
		/// ,
		/// <code>CICategoryInterlaced</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryCompositeOperation</code>
		/// <p></p><b>Localized Display Name</b>
		/// Color Dodge Blend Mode
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='backgroundImage'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter ColorDodgeBlendMode(Texture2D backgroundImage) {
			return Filter("CIColorDodgeBlendMode", new Dictionary<string, object>() {
				{"inputBackgroundImage",CIImage.FromTexture2D(backgroundImage)}
			});
		}

		/// <summary>Inverts the colors in an image.</summary>
		/// <remarks>
		/// <p></p><b>CIColorInvert</b>
		/// <p class="abstract">Inverts the colors in an image.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryNonSquarePixels</code>
		/// ,
		/// <code>CICategoryInterlaced</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryColorEffect</code>
		/// <p></p><b>Localized Display Name</b>
		/// Color Invert
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter ColorInvert() {
			return Filter("CIColorInvert", new Dictionary<string, object>() {
			});
		}

		/// <summary>Performs a nonlinear transformation of source color values using mapping values provided in a table.</summary>
		/// <remarks>
		/// <p></p><b>CIColorMap</b>
		/// <p class="abstract">Performs a nonlinear transformation of source color values using mapping values provided in a table.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>gradientImage</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>Texture2D</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeGradient</code>
		///         and whose display name is Gradient Image.
		///       </p>
		///     
		///   
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryNonSquarePixels</code>
		/// ,
		/// <code>CICategoryInterlaced</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryColorEffect</code>
		/// <p></p><b>Localized Display Name</b>
		/// Color Map
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='gradientImage'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter ColorMap(Texture2D gradientImage) {
			return Filter("CIColorMap", new Dictionary<string, object>() {
				{"inputGradientImage",CIImage.FromTexture2D(gradientImage)}
			});
		}

		/// <summary>Multiplies source color values and adds a bias factor to each color component.</summary>
		/// <remarks>
		/// <p></p><b>CIColorMatrix</b>
		/// <p class="abstract">Multiplies source color values and adds a bias factor to each color component.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>rVector</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose display name is Red Vector.
		///       </p>
		///       <p>Default value: [1 0 0 0]</p>
		///     
		///     
		///       <em>gVector</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose display name is Green Vector.
		///       </p>
		///       <p>Default value: [0 1 0 0]</p>
		///     
		///     
		///       <em>bVector</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose display name is Blue Vector.
		///       </p>
		///       <p>Default value: [0 0 1 0]</p>
		///     
		///     
		///       <em>aVector</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose display name is Alpha Vector.
		///       </p>
		///       <p>Default value: [0 0 0 1]</p>
		///     
		///     
		///       <em>biasVector</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose display name is Bias Vector.
		///       </p>
		///       <p>Default value: [0 0 0 0]</p>
		///     
		///   
		/// 
		/// 
		///   <p></p><b>Discussion</b>
		///   <p>This filter performs a matrix multiplication, as follows, to transform the color vector:</p>
		///   
		///     <table>
		///       <tr>
		///         <td scope="row">
		///           <pre>
		///             s.r = dot(s, redVector)
		///             <span/>
		///           </pre>
		///         </td>
		///       </tr>
		///       <tr>
		///         <td scope="row">
		///           <pre>
		///             s.g = dot(s, greenVector)
		///             <span/>
		///           </pre>
		///         </td>
		///       </tr>
		///       <tr>
		///         <td scope="row">
		///           <pre>
		///             s.b = dot(s, blueVector)
		///             <span/>
		///           </pre>
		///         </td>
		///       </tr>
		///       <tr>
		///         <td scope="row">
		///           <pre>
		///             s.a = dot(s, alphaVector)
		///             <span/>
		///           </pre>
		///         </td>
		///       </tr>
		///       <tr>
		///         <td scope="row">
		///           <pre>
		///             s = s + bias
		///             <span/>
		///           </pre>
		///         </td>
		///       </tr>
		///     </table>
		///   
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryNonSquarePixels</code>
		/// ,
		/// <code>CICategoryInterlaced</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryColorAdjustment</code>
		/// <p></p><b>Localized Display Name</b>
		/// Color Matrix
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 5.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='rVector'>An array of floats representing a vector</param>
		/// <param name='gVector'>An array of floats representing a vector</param>
		/// <param name='bVector'>An array of floats representing a vector</param>
		/// <param name='aVector'>An array of floats representing a vector</param>
		/// <param name='biasVector'>An array of floats representing a vector</param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter ColorMatrix(float[] rVector, float[] gVector, float[] bVector, float[] aVector, float[] biasVector) {
			return Filter("CIColorMatrix", new Dictionary<string, object>() {
				{"inputRVector",new CIVector("[" + string.Join(" ", Array.ConvertAll(rVector, x => x.ToString())) + "]")},
				{"inputGVector",new CIVector("[" + string.Join(" ", Array.ConvertAll(gVector, x => x.ToString())) + "]")},
				{"inputBVector",new CIVector("[" + string.Join(" ", Array.ConvertAll(bVector, x => x.ToString())) + "]")},
				{"inputAVector",new CIVector("[" + string.Join(" ", Array.ConvertAll(aVector, x => x.ToString())) + "]")},
				{"inputBiasVector",new CIVector("[" + string.Join(" ", Array.ConvertAll(biasVector, x => x.ToString())) + "]")}
			});
		}

		/// <summary>Remaps colors so they fall within shades of a single color.</summary>
		/// <remarks>
		/// <p></p><b>CIColorMonochrome</b>
		/// <p class="abstract">Remaps colors so they fall within shades of a single color.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>color</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>Color3</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeOpaqueColor</code>
		///         and whose display name is Color.
		///       </p>
		///     
		///     
		///       <em>intensity</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeScalar</code>
		///         and whose display name is Intensity.
		///       </p>
		///       <p>Default value: 1.00</p>
		///     
		///   
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryNonSquarePixels</code>
		/// ,
		/// <code>CICategoryInterlaced</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryColorEffect</code>
		/// <p></p><b>Localized Display Name</b>
		/// Color Monochrome
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='color'>A color in RGBA format</param>
		/// <param name='intensity'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter ColorMonochrome(Color32 color, float intensity) {
			return Filter("CIColorMonochrome", new Dictionary<string, object>() {
				{"inputColor",CIColor.FromColor32(color)},
				{"inputIntensity",intensity}
			});
		}

		/// <summary>Modifies the pixel values in an image by applying a set of cubic polynomials.</summary>
		/// <remarks>
		/// <p></p><b>CIColorPolynomial</b>
		/// <p class="abstract">Modifies the pixel values in an image by applying a set of cubic polynomials.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>redCoefficients</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeRectangle</code>
		///         and whose display name is RedCoefficients.
		///       </p>
		///       <p>Default value: [0 1 0 0] Identity: [0 1 0 0]</p>
		///     
		///     
		///       <em>greenCoefficients</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeRectangle</code>
		///         and whose display name is GreenCoefficients.
		///       </p>
		///       <p>Default value: [0 1 0 0] Identity: [0 1 0 0]</p>
		///     
		///     
		///       <em>blueCoefficients</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeRectangle</code>
		///         and whose display name is BlueCoefficients.
		///       </p>
		///       <p>Default value: [0 1 0 0] Identity: [0 1 0 0]</p>
		///     
		///     
		///       <em>alphaCoefficients</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeRectangle</code>
		///         and whose display name is AlphaCoefficients.
		///       </p>
		///       <p>Default value: [0 1 0 0] Identity: [0 1 0 0]</p>
		///     
		///   
		/// 
		/// 
		///   <p></p><b>Discussion</b>
		///   <p>For each pixel, the value of each color component is treated as the input to a cubic polynomial, whose coefficients are taken from the corresponding input coefficients parameter in ascending order. Equivalent to the following formula:</p>
		///   
		///     <table>
		///       <tr>
		///         <td scope="row">
		///           <pre>
		///             r = rCoeff[0] + rCoeff[1] * r + rCoeff[2] * r*r + rCoeff[3] * r*r*r
		///             <span/>
		///           </pre>
		///         </td>
		///       </tr>
		///       <tr>
		///         <td scope="row">
		///           <pre>
		///             g = gCoeff[0] + gCoeff[1] * g + gCoeff[2] * g*g + gCoeff[3] * g*g*g
		///             <span/>
		///           </pre>
		///         </td>
		///       </tr>
		///       <tr>
		///         <td scope="row">
		///           <pre>
		///             b = bCoeff[0] + bCoeff[1] * b + bCoeff[2] * b*b + bCoeff[3] * b*b*b
		///             <span/>
		///           </pre>
		///         </td>
		///       </tr>
		///       <tr>
		///         <td scope="row">
		///           <pre>
		///             a = aCoeff[0] + aCoeff[1] * a + aCoeff[2] * a*a + aCoeff[3] * a*a*a
		///             <span/>
		///           </pre>
		///         </td>
		///       </tr>
		///     </table>
		///   
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryNonSquarePixels</code>
		/// ,
		/// <code>CICategoryInterlaced</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryColorAdjustment</code>
		/// <p></p><b>Localized Display Name</b>
		/// Color Polynomial
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.9 and later and in iOS 7.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='redCoefficients'>An array of floats representing a vector</param>
		/// <param name='greenCoefficients'>An array of floats representing a vector</param>
		/// <param name='blueCoefficients'>An array of floats representing a vector</param>
		/// <param name='alphaCoefficients'>An array of floats representing a vector</param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter ColorPolynomial(float[] redCoefficients, float[] greenCoefficients, float[] blueCoefficients, float[] alphaCoefficients) {
			return Filter("CIColorPolynomial", new Dictionary<string, object>() {
				{"inputRedCoefficients",new CIVector("[" + string.Join(" ", Array.ConvertAll(redCoefficients, x => x.ToString())) + "]")},
				{"inputGreenCoefficients",new CIVector("[" + string.Join(" ", Array.ConvertAll(greenCoefficients, x => x.ToString())) + "]")},
				{"inputBlueCoefficients",new CIVector("[" + string.Join(" ", Array.ConvertAll(blueCoefficients, x => x.ToString())) + "]")},
				{"inputAlphaCoefficients",new CIVector("[" + string.Join(" ", Array.ConvertAll(alphaCoefficients, x => x.ToString())) + "]")}
			});
		}

		/// <summary>Remaps red, green, and blue color components to the number of brightness values you specify for each color component.</summary>
		/// <remarks>
		/// <p></p><b>CIColorPosterize</b>
		/// <p class="abstract">Remaps red, green, and blue color components to the number of brightness values you specify for each color component.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>levels</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeScalar</code>
		///         and whose display name is Levels.
		///       </p>
		///       <p>Default value: 6.00</p>
		///     
		///   
		/// 
		/// 
		///   <p></p><b>Discussion</b>
		///   <p>This filter flattens colors to achieve a look similar to that of a silk-screened poster.</p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryNonSquarePixels</code>
		/// ,
		/// <code>CICategoryInterlaced</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryColorEffect</code>
		/// <p></p><b>Localized Display Name</b>
		/// Color Posterize
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='levels'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter ColorPosterize(float levels) {
			return Filter("CIColorPosterize", new Dictionary<string, object>() {
				{"inputLevels",levels}
			});
		}

		/// <summary>Generates a solid color.</summary>
		/// <remarks>
		/// <p></p><b>CIConstantColorGenerator</b>
		/// <p class="abstract">Generates a solid color.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		/// 
		/// 
		///   <p></p><b>Discussion</b>
		///   <p>You typically use the output of this filter as the input to another filter.</p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryGenerator</code>
		/// <p></p><b>Localized Display Name</b>
		/// Constant Color
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 5.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter ConstantColorGenerator() {
			return Filter("CIConstantColorGenerator", new Dictionary<string, object>() {
			});
		}

		/// <summary>Modifies pixel values by performing a 3x3 matrix convolution.</summary>
		/// <remarks>
		/// <p></p><b>CIConvolution3X3</b>
		/// <p class="abstract">Modifies pixel values by performing a 3x3 matrix convolution.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>weights</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose display name is Weights.
		///       </p>
		///       <p>Default value: [0 0 0 0 1 0 0 0 0] Identity: [0 0 0 0 1 0 0 0 0]</p>
		///     
		///     
		///       <em>bias</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeDistance</code>
		///         and whose display name is Bias.
		///       </p>
		///       <p>Default value: 0.00 Identity: 0.00</p>
		///     
		///   
		/// 
		/// 
		///   <p></p><b>Discussion</b>
		///   <p>
		///     A convolution filter generates each output pixel by summing all elements in the element-wise product of two matrices—a weight matrix and a matrix containing the neighborhood surrounding the corresponding input pixel—and adding a bias. This operation is performed independently for each color component (including the alpha component), and the resulting value is clamped to the range between
		///     <code>0.0</code>
		///     and
		///     <code>1.0</code>
		///     . You can create many types of image processing effects using different weight matrices, such as blurring, sharpening, edge detection, translation, and embossing.
		///   </p>
		///   <p>This filter uses a 3x3 weight matrix and the 3x3 neighborhood surrounding an input pixel (that is, the pixel itself and those within a distance of one pixel horizontally or vertically).</p>
		///   <p>
		///     If you want to preserve the overall brightness of the image, ensure that the sum of all values in the weight matrix is
		///     <code>1.0</code>
		///     . You may find it convenient to devise a weight matrix without this constraint and then normalize it by dividing each element by the sum of all elements, as shown in the figure below.
		///   </p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryStylize</code>
		/// <p></p><b>Localized Display Name</b>
		/// 3 by 3 convolution
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.9 and later and in iOS 7.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='weights'>An array of floats representing a vector</param>
		/// <param name='bias'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter Convolution3X3(float[] weights, float bias) {
			return Filter("CIConvolution3X3", new Dictionary<string, object>() {
				{"inputWeights",new CIVector("[" + string.Join(" ", Array.ConvertAll(weights, x => x.ToString())) + "]")},
				{"inputBias",bias}
			});
		}

		/// <summary>Modifies pixel values by performing a 5x5 matrix convolution.</summary>
		/// <remarks>
		/// <p></p><b>CIConvolution5X5</b>
		/// <p class="abstract">Modifies pixel values by performing a 5x5 matrix convolution.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>weights</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose display name is Weights.
		///       </p>
		///       <p>Default value: [0 0 0 0 0 0 0 0 0 0 0 0 1 0 0 0 0 0 0 0 0 0 0 0 0] Identity: [0 0 0 0 0 0 0 0 0 0 0 0 1 0 0 0 0 0 0 0 0 0 0 0 0]</p>
		///     
		///     
		///       <em>bias</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeDistance</code>
		///         and whose display name is Bias.
		///       </p>
		///       <p>Default value: 0.00 Identity: 0.00</p>
		///     
		///   
		/// 
		/// 
		///   <p></p><b>Discussion</b>
		///   <p>
		///     A convolution filter generates each output pixel by summing all elements in the element-wise product of two matrices—a weight matrix and a matrix containing the neighborhood surrounding the corresponding input pixel—and adding a bias. This operation is performed independently for each color component (including the alpha component), and the resulting value is clamped to the range between
		///     <code>0.0</code>
		///     and
		///     <code>1.0</code>
		///     . You can create many types of image processing effects using different weight matrices, such as blurring, sharpening, edge detection, translation, and embossing.
		///   </p>
		///   <p>This filter uses a 5x5 weight matrix and the 5x5 neighborhood surrounding an input pixel (that is, the pixel itself and those within a distance of two pixels horizontally or vertically).</p>
		///   <p>
		///     If you want to preserve the overall brightness of the image, ensure that the sum of all values in the weight matrix is
		///     <code>1.0</code>
		///     . You may find it convenient to devise a weight matrix without this constraint and then normalize it by dividing each element by the sum of all elements.
		///   </p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryStylize</code>
		/// <p></p><b>Localized Display Name</b>
		/// 5 by 5 convolution
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.9 and later and in iOS 7.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='weights'>An array of floats representing a vector</param>
		/// <param name='bias'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter Convolution5X5(float[] weights, float bias) {
			return Filter("CIConvolution5X5", new Dictionary<string, object>() {
				{"inputWeights",new CIVector("[" + string.Join(" ", Array.ConvertAll(weights, x => x.ToString())) + "]")},
				{"inputBias",bias}
			});
		}

		/// <summary>Modifies pixel values by performing a 7x7 matrix convolution.</summary>
		/// <remarks>
		/// <p></p><b>CIConvolution7X7</b>
		/// <p class="abstract">Modifies pixel values by performing a 7x7 matrix convolution.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>weights</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose display name is Weights.
		///       </p>
		///       <p>Default value: [0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 1 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0]</p>
		///       <p>Identity: [0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 1 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0]</p>
		///     
		///     
		///       <em>bias</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeDistance</code>
		///         and whose display name is Bias.
		///       </p>
		///       <p>Default value: 0.00 Identity: 0.00</p>
		///     
		///   
		/// 
		/// 
		///   <p></p><b>Discussion</b>
		///   <p>
		///     A convolution filter generates each output pixel by summing all elements in the element-wise product of two matrices—a weight matrix and a matrix containing the neighborhood surrounding the corresponding input pixel—and adding a bias. This operation is performed independently for each color component (including the alpha component), and the resulting value is clamped to the range between
		///     <code>0.0</code>
		///     and
		///     <code>1.0</code>
		///     . You can create many types of image processing effects using different weight matrices, such as blurring, sharpening, edge detection, translation, and embossing.
		///   </p>
		///   <p>This filter uses a 7x7 weight matrix and the 7x7 neighborhood surrounding an input pixel (that is, the pixel itself and those within a distance of three pixels horizontally or vertically).</p>
		///   <p>
		///     If you want to preserve the overall brightness of the image, ensure that the sum of all values in the weight matrix is
		///     <code>1.0</code>
		///     . You may find it convenient to devise a weight matrix without this constraint and then normalize it by dividing each element by the sum of all elements.
		///   </p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryStylize</code>
		/// <p></p><b>Localized Display Name</b>
		/// 7 by 7 convolution
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.9 and later and in iOS 7.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='weights'>An array of floats representing a vector</param>
		/// <param name='bias'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter Convolution7X7(float[] weights, float bias) {
			return Filter("CIConvolution7X7", new Dictionary<string, object>() {
				{"inputWeights",new CIVector("[" + string.Join(" ", Array.ConvertAll(weights, x => x.ToString())) + "]")},
				{"inputBias",bias}
			});
		}

		/// <summary>Modifies pixel values by performing a 9-element horizontal convolution.</summary>
		/// <remarks>
		/// <p></p><b>CIConvolution9Horizontal</b>
		/// <p class="abstract">Modifies pixel values by performing a 9-element horizontal convolution.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>weights</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose display name is Weights.
		///       </p>
		///       <p>Default value: [0 0 0 0 1 0 0 0 0] Identity: [0 0 0 0 1 0 0 0 0]</p>
		///     
		///     
		///       <em>bias</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeDistance</code>
		///         and whose display name is Bias.
		///       </p>
		///       <p>Default value: 0.00 Identity: 0.00</p>
		///     
		///   
		/// 
		/// 
		///   <p></p><b>Discussion</b>
		///   <p>
		///     A convolution filter generates each output pixel by summing all elements in the element-wise product of two matrices—a weight matrix and a matrix containing the neighborhood surrounding the corresponding input pixel—and adding a bias. This operation is performed independently for each color component (including the alpha component), and the resulting value is clamped to the range between
		///     <code>0.0</code>
		///     and
		///     <code>1.0</code>
		///     . You can create many types of image processing effects using different weight matrices, such as blurring, sharpening, edge detection, translation, and embossing.
		///   </p>
		///   <p>
		///     This filter uses a 9x1 weight matrix and the 9x1 neighborhood surrounding an input pixel (that is, the pixel itself and those within a distance of four pixels horizontally). Unlike convolution filters which use square matrices, this filter can only produce effects along a horizontal axis, but it can be combined with
		///     <code>
		///       <a href="#//apple_ref/doc/filter/ci/CIConvolution9Vertical">CIConvolution9Vertical</a>
		///     </code>
		///     to approximate the effect of certain 9x9 weight matrices.
		///   </p>
		///   <p>
		///     If you want to preserve the overall brightness of the image, ensure that the sum of all values in the weight matrix is
		///     <code>1.0</code>
		///     . You may find it convenient to devise a weight matrix without this constraint and then normalize it by dividing each element by the sum of all elements.
		///   </p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryStylize</code>
		/// <p></p><b>Localized Display Name</b>
		/// CIConvolution9Horizontal
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.9 and later and in iOS 7.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='weights'>An array of floats representing a vector</param>
		/// <param name='bias'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter Convolution9Horizontal(float[] weights, float bias) {
			return Filter("CIConvolution9Horizontal", new Dictionary<string, object>() {
				{"inputWeights",new CIVector("[" + string.Join(" ", Array.ConvertAll(weights, x => x.ToString())) + "]")},
				{"inputBias",bias}
			});
		}

		/// <summary>Modifies pixel values by performing a 9-element vertical convolution.</summary>
		/// <remarks>
		/// <p></p><b>CIConvolution9Vertical</b>
		/// <p class="abstract">Modifies pixel values by performing a 9-element vertical convolution.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>weights</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose display name is Weights.
		///       </p>
		///       <p>Default value: [0 0 0 0 1 0 0 0 0] Identity: [0 0 0 0 1 0 0 0 0]</p>
		///     
		///     
		///       <em>bias</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeDistance</code>
		///         and whose display name is Bias.
		///       </p>
		///       <p>Default value: 0.00 Identity: 0.00</p>
		///     
		///   
		/// 
		/// 
		///   <p></p><b>Discussion</b>
		///   <p>
		///     A convolution filter generates each output pixel by summing all elements in the element-wise product of two matrices—a weight matrix and a matrix containing the neighborhood surrounding the corresponding input pixel—and adding a bias. This operation is performed independently for each color component (including the alpha component), and the resulting value is clamped to the range between
		///     <code>0.0</code>
		///     and
		///     <code>1.0</code>
		///     . You can create many types of image processing effects using different weight matrices, such as blurring, sharpening, edge detection, translation, and embossing.
		///   </p>
		///   <p>
		///     This filter uses a 1x9 weight matrix and the 1x9 neighborhood surrounding an input pixel (that is, the pixel itself and those within a distance of four pixels vertically). Unlike convolution filters which use square matrices, this filter can only produce effects along a vertical axis, but it can be combined with
		///     <code>
		///       <a href="#//apple_ref/doc/filter/ci/CIConvolution9Vertical">CIConvolution9Vertical</a>
		///     </code>
		///     to approximate the effect of certain 9x9 weight matrices.
		///   </p>
		///   <p>
		///     If you want to preserve the overall brightness of the image, ensure that the sum of all values in the weight matrix is
		///     <code>1.0</code>
		///     . You may find it convenient to devise a weight matrix without this constraint and then normalize it by dividing each element by the sum of all elements.
		///   </p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryStylize</code>
		/// <p></p><b>Localized Display Name</b>
		/// CIConvolution9Vertical
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.9 and later and in iOS 7.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='weights'>An array of floats representing a vector</param>
		/// <param name='bias'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter Convolution9Vertical(float[] weights, float bias) {
			return Filter("CIConvolution9Vertical", new Dictionary<string, object>() {
				{"inputWeights",new CIVector("[" + string.Join(" ", Array.ConvertAll(weights, x => x.ToString())) + "]")},
				{"inputBias",bias}
			});
		}

		/// <summary>Transitions from one image to another by simulating the effect of a copy machine.</summary>
		/// <remarks>
		/// <p></p><b>CICopyMachineTransition</b>
		/// <p class="abstract">Transitions from one image to another by simulating the effect of a copy machine.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>targetImage</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>Texture2D</c>
		///         object whose display name is Target Image.
		///       </p>
		///     
		///     
		///       <em>extent</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeRectangle</code>
		///         and whose display name is Extent.
		///       </p>
		///       <p>Default value: [0 0 300 300]</p>
		///     
		///     
		///       <em>color</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>Color3</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeOpaqueColor</code>
		///         and whose display name is Color.
		///       </p>
		///     
		///     
		///       <em>time</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeTime</code>
		///         and whose display name is Time.
		///       </p>
		///       <p>Default value: 0.00</p>
		///     
		///     
		///       <em>angle</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeAngle</code>
		///         and whose display name is Angle.
		///       </p>
		///       <p>Default value: 0.00</p>
		///     
		///     
		///       <em>width</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeDistance</code>
		///         and whose display name is Width.
		///       </p>
		///       <p>Default value: 200.00</p>
		///     
		///     
		///       <em>opacity</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeScalar</code>
		///         and whose display name is Opacity.
		///       </p>
		///       <p>Default value: 1.30</p>
		///     
		///   
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryTransition</code>
		/// <p></p><b>Localized Display Name</b>
		/// Copy Machine
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='targetImage'></param>
		/// <param name='extent'>An array of floats representing a vector</param>
		/// <param name='color'>A color in RGBA format</param>
		/// <param name='time'></param>
		/// <param name='angle'></param>
		/// <param name='width'></param>
		/// <param name='opacity'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter CopyMachineTransition(Texture2D targetImage, float[] extent, Color32 color, float time, float angle, float width, float opacity) {
			return Filter("CICopyMachineTransition", new Dictionary<string, object>() {
				{"inputTargetImage",CIImage.FromTexture2D(targetImage)},
				{"inputExtent",new CIVector("[" + string.Join(" ", Array.ConvertAll(extent, x => x.ToString())) + "]")},
				{"inputColor",CIColor.FromColor32(color)},
				{"inputTime",time},
				{"inputAngle",angle},
				{"inputWidth",width},
				{"inputOpacity",opacity}
			});
		}

		/// <summary>Applies a crop to an image.</summary>
		/// <remarks>
		/// <p></p><b>CICrop</b>
		/// <p class="abstract">Applies a crop to an image.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>rectangle</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeRectangle</code>
		///         and whose display name is Rectangle.
		///       </p>
		///       <p>Default value: [0 0 300 300]</p>
		///     
		///   
		/// 
		/// 
		///   <p></p><b>Discussion</b>
		///   <p>The size and shape of the cropped image depend on the rectangle you specify.</p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryGeometryAdjustment</code>
		/// <p></p><b>Localized Display Name</b>
		/// Crop
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 5.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='rectangle'>An array of floats representing a vector</param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter Crop(float[] rectangle) {
			return Filter("CICrop", new Dictionary<string, object>() {
				{"inputRectangle",new CIVector("[" + string.Join(" ", Array.ConvertAll(rectangle, x => x.ToString())) + "]")}
			});
		}

		/// <summary>Creates composite image samples by choosing the darker samples (from either the source image or the background).</summary>
		/// <remarks>
		/// <p></p><b>CIDarkenBlendMode</b>
		/// <p class="abstract">Creates composite image samples by choosing the darker samples (from either the source image or the background).</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>backgroundImage</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>Texture2D</c>
		///         object whose display name is Background Image.
		///       </p>
		///     
		///   
		/// 
		/// 
		///   <p></p><b>Discussion</b>
		///   <p>
		///     The result is that the background image samples are replaced by any source image samples that are darker. Otherwise, the background image samples are left unchanged. The formula used to create this filter is described in the PDF specification, which is available online from the Adobe Developer Center. See
		///     <span class="content_text">
		///       <a href="http://www.adobe.com/devnet/pdf/pdf_reference.html" class="urlLink" rel="external">PDF Reference and Adobe Extensions to the PDF Specification</a>
		///     </span>
		///     .
		///   </p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryNonSquarePixels</code>
		/// ,
		/// <code>CICategoryInterlaced</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryCompositeOperation</code>
		/// <p></p><b>Localized Display Name</b>
		/// Darken Blend Mode
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='backgroundImage'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter DarkenBlendMode(Texture2D backgroundImage) {
			return Filter("CIDarkenBlendMode", new Dictionary<string, object>() {
				{"inputBackgroundImage",CIImage.FromTexture2D(backgroundImage)}
			});
		}

		/// <summary>Subtracts either the source image sample color from the background image sample color, or the reverse, depending on which sample has the greater brightness value.</summary>
		/// <remarks>
		/// <p></p><b>CIDifferenceBlendMode</b>
		/// <p class="abstract">Subtracts either the source image sample color from the background image sample color, or the reverse, depending on which sample has the greater brightness value.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>backgroundImage</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>Texture2D</c>
		///         object whose display name is Background Image.
		///       </p>
		///     
		///   
		/// 
		/// 
		///   <p></p><b>Discussion</b>
		///   <p>
		///     Source image sample values that are black produce no change; white inverts the background color values. The formula used to create this filter is described in the PDF specification, which is available online from the Adobe Developer Center. See
		///     <span class="content_text">
		///       <a href="http://www.adobe.com/devnet/pdf/pdf_reference.html" class="urlLink" rel="external">PDF Reference and Adobe Extensions to the PDF Specification</a>
		///     </span>
		///     .
		///   </p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryNonSquarePixels</code>
		/// ,
		/// <code>CICategoryInterlaced</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryCompositeOperation</code>
		/// <p></p><b>Localized Display Name</b>
		/// Difference Blend Mode
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='backgroundImage'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter DifferenceBlendMode(Texture2D backgroundImage) {
			return Filter("CIDifferenceBlendMode", new Dictionary<string, object>() {
				{"inputBackgroundImage",CIImage.FromTexture2D(backgroundImage)}
			});
		}

		/// <summary>Transitions from one image to another using the shape defined by a mask.</summary>
		/// <remarks>
		/// <p></p><b>CIDisintegrateWithMaskTransition</b>
		/// <p class="abstract">Transitions from one image to another using the shape defined by a mask.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>targetImage</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>Texture2D</c>
		///         object whose display name is Target Image.
		///       </p>
		///     
		///     
		///       <em>maskImage</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>Texture2D</c>
		///         object whose display name is Mask Image.
		///       </p>
		///     
		///     
		///       <em>time</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeTime</code>
		///         and whose display name is Time.
		///       </p>
		///       <p>Default value: 0.00</p>
		///     
		///     
		///       <em>shadowRadius</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeDistance</code>
		///         and whose display name is Shadow Radius.
		///       </p>
		///       <p>Default value: 8.00</p>
		///     
		///     
		///       <em>shadowDensity</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeScalar</code>
		///         and whose display name is Shadow Density.
		///       </p>
		///       <p>Default value: 0.65</p>
		///     
		///     
		///       <em>shadowOffset</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeOffset</code>
		///         and whose display name is Shadow Offset.
		///       </p>
		///       <p>Default value: [0 -10]</p>
		///     
		///   
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryTransition</code>
		/// <p></p><b>Localized Display Name</b>
		/// Disintegrate with Mask
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='targetImage'></param>
		/// <param name='maskImage'></param>
		/// <param name='time'></param>
		/// <param name='shadowRadius'></param>
		/// <param name='shadowDensity'></param>
		/// <param name='shadowOffset'>An array of floats representing a vector</param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter DisintegrateWithMaskTransition(Texture2D targetImage, Texture2D maskImage, float time, float shadowRadius, float shadowDensity, float[] shadowOffset) {
			return Filter("CIDisintegrateWithMaskTransition", new Dictionary<string, object>() {
				{"inputTargetImage",CIImage.FromTexture2D(targetImage)},
				{"inputMaskImage",CIImage.FromTexture2D(maskImage)},
				{"inputTime",time},
				{"inputShadowRadius",shadowRadius},
				{"inputShadowDensity",shadowDensity},
				{"inputShadowOffset",new CIVector("[" + string.Join(" ", Array.ConvertAll(shadowOffset, x => x.ToString())) + "]")}
			});
		}

		/// <summary>Uses a dissolve to transition from one image to another.</summary>
		/// <remarks>
		/// <p></p><b>CIDissolveTransition</b>
		/// <p class="abstract">Uses a dissolve to transition from one image to another.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>targetImage</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>Texture2D</c>
		///         object whose display name is Target Image.
		///       </p>
		///     
		///     
		///       <em>time</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeTime</code>
		///         and whose display name is Time.
		///       </p>
		///       <p>Default value: 0.00</p>
		///     
		///   
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryNonSquarePixels</code>
		/// ,
		/// <code>CICategoryInterlaced</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryTransition</code>
		/// <p></p><b>Localized Display Name</b>
		/// Dissolve
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='targetImage'></param>
		/// <param name='time'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter DissolveTransition(Texture2D targetImage, float time) {
			return Filter("CIDissolveTransition", new Dictionary<string, object>() {
				{"inputTargetImage",CIImage.FromTexture2D(targetImage)},
				{"inputTime",time}
			});
		}

		/// <summary>Simulates the dot patterns of a halftone screen.</summary>
		/// <remarks>
		/// <p></p><b>CIDotScreen</b>
		/// <p class="abstract">Simulates the dot patterns of a halftone screen.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>center</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypePosition</code>
		///         and whose display name is Center.
		///       </p>
		///       <p>Default value: [150 150]</p>
		///     
		///     
		///       <em>angle</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeAngle</code>
		///         and whose display name is Angle.
		///       </p>
		///       <p>Default value: 0.00</p>
		///     
		///     
		///       <em>width</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeDistance</code>
		///         and whose display name is Width.
		///       </p>
		///       <p>Default value: 6.00</p>
		///     
		///     
		///       <em>sharpness</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeScalar</code>
		///         and whose display name is Sharpness.
		///       </p>
		///       <p>Default value: 0.70</p>
		///     
		///   
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryHalftoneEffect</code>
		/// <p></p><b>Localized Display Name</b>
		/// Dot Screen
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='center'>An array of floats representing a vector</param>
		/// <param name='angle'></param>
		/// <param name='width'></param>
		/// <param name='sharpness'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter DotScreen(float[] center, float angle, float width, float sharpness) {
			return Filter("CIDotScreen", new Dictionary<string, object>() {
				{"inputCenter",new CIVector("[" + string.Join(" ", Array.ConvertAll(center, x => x.ToString())) + "]")},
				{"inputAngle",angle},
				{"inputWidth",width},
				{"inputSharpness",sharpness}
			});
		}

		/// <summary>Produces a tiled image from a source image by applying an 8-way reflected symmetry.</summary>
		/// <remarks>
		/// <p></p><b>CIEightfoldReflectedTile</b>
		/// <p class="abstract">Produces a tiled image from a source image by applying an 8-way reflected symmetry.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>center</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypePosition</code>
		///         and whose display name is Center.
		///       </p>
		///       <p>Default value: [150 150]</p>
		///     
		///     
		///       <em>angle</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeAngle</code>
		///         and whose display name is Angle.
		///       </p>
		///       <p>Default value: 0.00</p>
		///     
		///     
		///       <em>width</em>
		///     
		///     
		///       <p>
		///         The width, along with the
		///         <code>inputCenter</code>
		///         parameter, defines the portion of the image to tile. An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeDistance</code>
		///         and whose display name is Width.
		///       </p>
		///       <p>Default value: 100.00</p>
		///     
		///   
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryTileEffect</code>
		/// <p></p><b>Localized Display Name</b>
		/// CIEightfoldReflectedTile
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.5 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='center'>An array of floats representing a vector</param>
		/// <param name='angle'></param>
		/// <param name='width'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter EightfoldReflectedTile(float[] center, float angle, float width) {
			return Filter("CIEightfoldReflectedTile", new Dictionary<string, object>() {
				{"inputCenter",new CIVector("[" + string.Join(" ", Array.ConvertAll(center, x => x.ToString())) + "]")},
				{"inputAngle",angle},
				{"inputWidth",width}
			});
		}

		/// <summary>Produces an effect similar to that produced by the CIDifferenceBlendMode filter but with lower contrast.</summary>
		/// <remarks>
		/// <p></p><b>CIExclusionBlendMode</b>
		/// <p class="abstract">Produces an effect similar to that produced by the CIDifferenceBlendMode filter but with lower contrast.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>backgroundImage</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>Texture2D</c>
		///         object whose display name is Background Image.
		///       </p>
		///     
		///   
		/// 
		/// 
		///   <p></p><b>Discussion</b>
		///   <p>
		///     Source image sample values that are black do not produce a change; white inverts the background color values. The formula used to create this filter is described in the PDF specification, which is available online from the Adobe Developer Center. See
		///     <span class="content_text">
		///       <a href="http://www.adobe.com/devnet/pdf/pdf_reference.html" class="urlLink" rel="external">PDF Reference and Adobe Extensions to the PDF Specification</a>
		///     </span>
		///     .
		///   </p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryNonSquarePixels</code>
		/// ,
		/// <code>CICategoryInterlaced</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryCompositeOperation</code>
		/// <p></p><b>Localized Display Name</b>
		/// Exclusion Blend Mode
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='backgroundImage'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter ExclusionBlendMode(Texture2D backgroundImage) {
			return Filter("CIExclusionBlendMode", new Dictionary<string, object>() {
				{"inputBackgroundImage",CIImage.FromTexture2D(backgroundImage)}
			});
		}

		/// <summary>Adjusts the exposure setting for an image similar to the way you control exposure for a camera when you change the F-stop.</summary>
		/// <remarks>
		/// <p></p><b>CIExposureAdjust</b>
		/// <p class="abstract">Adjusts the exposure setting for an image similar to the way you control exposure for a camera when you change the F-stop.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>eV</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeScalar</code>
		///         and whose display name is EV.
		///       </p>
		///       <p>Default value: 0.50</p>
		///     
		///   
		/// 
		/// 
		///   <p></p><b>Discussion</b>
		///   <p>This filter multiplies the color values, as follows, to simulate exposure change by the specified F-stops:</p>
		///   <p>
		///     <code>s.rgb * pow(2.0, ev)</code>
		///   </p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryNonSquarePixels</code>
		/// ,
		/// <code>CICategoryInterlaced</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryColorAdjustment</code>
		/// <p></p><b>Localized Display Name</b>
		/// Exposure Adjust
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 5.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='eV'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter ExposureAdjust(float eV) {
			return Filter("CIExposureAdjust", new Dictionary<string, object>() {
				{"inputEV",eV}
			});
		}

		/// <summary>Maps luminance to a color ramp of two colors.</summary>
		/// <remarks>
		/// <p></p><b>CIFalseColor</b>
		/// <p class="abstract">Maps luminance to a color ramp of two colors.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>color0</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>Color3</c>
		///         object whose display name is Color 1.
		///       </p>
		///     
		///     
		///       <em>color1</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>Color3</c>
		///         object whose display name is Color 2.
		///       </p>
		///     
		///   
		/// 
		/// 
		///   <p></p><b>Discussion</b>
		///   <p>False color is often used to process astronomical and other scientific data, such as ultraviolet and x-ray images.</p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryNonSquarePixels</code>
		/// ,
		/// <code>CICategoryInterlaced</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryColorEffect</code>
		/// <p></p><b>Localized Display Name</b>
		/// False Color
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='color0'>A color in RGBA format</param>
		/// <param name='color1'>A color in RGBA format</param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter FalseColor(Color32 color0, Color32 color1) {
			return Filter("CIFalseColor", new Dictionary<string, object>() {
				{"inputColor0",CIColor.FromColor32(color0)},
				{"inputColor1",CIColor.FromColor32(color1)}
			});
		}

		/// <summary>Transitions from one image to another by creating a flash.</summary>
		/// <remarks>
		/// <p></p><b>CIFlashTransition</b>
		/// <p class="abstract">Transitions from one image to another by creating a flash.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>targetImage</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>Texture2D</c>
		///         object whose display name is Target Image.
		///       </p>
		///     
		///     
		///       <em>center</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypePosition</code>
		///         and whose display name is Center.
		///       </p>
		///       <p>Default value: [150 150]</p>
		///     
		///     
		///       <em>extent</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeRectangle</code>
		///         and whose display name is Extent.
		///       </p>
		///       <p>Default value: [0 0 300 300]</p>
		///     
		///     
		///       <em>color</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>Color3</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeOpaqueColor</code>
		///         and whose display name is Color.
		///       </p>
		///     
		///     
		///       <em>time</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeTime</code>
		///         and whose display name is Time.
		///       </p>
		///       <p>Default value: 0.00</p>
		///     
		///     
		///       <em>maxStriationRadius</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeScalar</code>
		///         and whose display name is Maximum Striation Radius.
		///       </p>
		///       <p>Default value: 2.58</p>
		///     
		///     
		///       <em>striationStrength</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeScalar</code>
		///         and whose display name is Striation Strength.
		///       </p>
		///       <p>Default value: 0.50</p>
		///     
		///     
		///       <em>striationContrast</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeScalar</code>
		///         and whose display name is Striation Contrast.
		///       </p>
		///       <p>Default value: 1.38</p>
		///     
		///     
		///       <em>fadeThreshold</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeScalar</code>
		///         and whose display name is Fade Threshold.
		///       </p>
		///       <p>Default value: 0.85</p>
		///     
		///   
		/// 
		/// 
		///   <p></p><b>Discussion</b>
		///   <p>The flash originates from a point you specify. Small at first, it rapidly expands until the image frame is completely filled with the flash color. As the color fades, the target image begins to appear.</p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryTransition</code>
		/// <p></p><b>Localized Display Name</b>
		/// Flash
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='targetImage'></param>
		/// <param name='center'>An array of floats representing a vector</param>
		/// <param name='extent'>An array of floats representing a vector</param>
		/// <param name='color'>A color in RGBA format</param>
		/// <param name='time'></param>
		/// <param name='maxStriationRadius'></param>
		/// <param name='striationStrength'></param>
		/// <param name='striationContrast'></param>
		/// <param name='fadeThreshold'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter FlashTransition(Texture2D targetImage, float[] center, float[] extent, Color32 color, float time, float maxStriationRadius, float striationStrength, float striationContrast, float fadeThreshold) {
			return Filter("CIFlashTransition", new Dictionary<string, object>() {
				{"inputTargetImage",CIImage.FromTexture2D(targetImage)},
				{"inputCenter",new CIVector("[" + string.Join(" ", Array.ConvertAll(center, x => x.ToString())) + "]")},
				{"inputExtent",new CIVector("[" + string.Join(" ", Array.ConvertAll(extent, x => x.ToString())) + "]")},
				{"inputColor",CIColor.FromColor32(color)},
				{"inputTime",time},
				{"inputMaxStriationRadius",maxStriationRadius},
				{"inputStriationStrength",striationStrength},
				{"inputStriationContrast",striationContrast},
				{"inputFadeThreshold",fadeThreshold}
			});
		}

		/// <summary>Produces a tiled image from a source image by applying a 4-way reflected symmetry.</summary>
		/// <remarks>
		/// <p></p><b>CIFourfoldReflectedTile</b>
		/// <p class="abstract">Produces a tiled image from a source image by applying a 4-way reflected symmetry.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>center</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypePosition</code>
		///         and whose display name is Center.
		///       </p>
		///       <p>Default value: [150 150]</p>
		///     
		///     
		///       <em>angle</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeAngle</code>
		///         and whose display name is Angle.
		///       </p>
		///       <p>Default value: 0.00</p>
		///     
		///     
		///       <em>acuteAngle</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeAngle</code>
		///         and whose display name is Acute Angle.
		///       </p>
		///       <p>Default value: 1.57</p>
		///     
		///     
		///       <em>width</em>
		///     
		///     
		///       <p>
		///         The width, along with the
		///         <code>inputCenter</code>
		///         parameter, defines the portion of the image to tile. An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeDistance</code>
		///         and whose display name is Width.
		///       </p>
		///       <p>Default value: 100.00</p>
		///     
		///   
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryTileEffect</code>
		/// <p></p><b>Localized Display Name</b>
		/// CIFourfoldReflectedTile
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.5 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='center'>An array of floats representing a vector</param>
		/// <param name='angle'></param>
		/// <param name='acuteAngle'></param>
		/// <param name='width'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter FourfoldReflectedTile(float[] center, float angle, float acuteAngle, float width) {
			return Filter("CIFourfoldReflectedTile", new Dictionary<string, object>() {
				{"inputCenter",new CIVector("[" + string.Join(" ", Array.ConvertAll(center, x => x.ToString())) + "]")},
				{"inputAngle",angle},
				{"inputAcuteAngle",acuteAngle},
				{"inputWidth",width}
			});
		}

		/// <summary>Produces a tiled image from a source image by rotating the source image at increments of 90 degrees.</summary>
		/// <remarks>
		/// <p></p><b>CIFourfoldRotatedTile</b>
		/// <p class="abstract">Produces a tiled image from a source image by rotating the source image at increments of 90 degrees.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>center</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypePosition</code>
		///         and whose display name is Center.
		///       </p>
		///       <p>Default value: [150 150]</p>
		///     
		///     
		///       <em>angle</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeAngle</code>
		///         and whose display name is Angle.
		///       </p>
		///       <p>Default value: 0.00</p>
		///     
		///     
		///       <em>width</em>
		///     
		///     
		///       <p>
		///         The width, along with the
		///         <code>inputCenter</code>
		///         parameter, defines the portion of the image to tile. An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeDistance</code>
		///         and whose display name is Width.
		///       </p>
		///       <p>Default value: 100.00</p>
		///     
		///   
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryTileEffect</code>
		/// <p></p><b>Localized Display Name</b>
		/// CIFourfoldRotatedTile
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.5 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='center'>An array of floats representing a vector</param>
		/// <param name='angle'></param>
		/// <param name='width'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter FourfoldRotatedTile(float[] center, float angle, float width) {
			return Filter("CIFourfoldRotatedTile", new Dictionary<string, object>() {
				{"inputCenter",new CIVector("[" + string.Join(" ", Array.ConvertAll(center, x => x.ToString())) + "]")},
				{"inputAngle",angle},
				{"inputWidth",width}
			});
		}

		/// <summary>Produces a tiled image from a source image by applying 4 translation operations.</summary>
		/// <remarks>
		/// <p></p><b>CIFourfoldTranslatedTile</b>
		/// <p class="abstract">Produces a tiled image from a source image by applying 4 translation operations.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>center</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypePosition</code>
		///         and whose display name is Center.
		///       </p>
		///       <p>Default value: [150 150]</p>
		///     
		///     
		///       <em>angle</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeAngle</code>
		///         and whose display name is Angle.
		///       </p>
		///       <p>Default value: 0.00</p>
		///     
		///     
		///       <em>acuteAngle</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeAngle</code>
		///         and whose display name is Acute Angle.
		///       </p>
		///       <p>Default value: 1.57</p>
		///     
		///     
		///       <em>width</em>
		///     
		///     
		///       <p>
		///         The width, along with the
		///         <code>inputCenter</code>
		///         parameter, defines the portion of the image to tile. An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeDistance</code>
		///         and whose display name is Width.
		///       </p>
		///       <p>Default value: 100.00</p>
		///     
		///   
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryTileEffect</code>
		/// <p></p><b>Localized Display Name</b>
		/// CIFourfoldTranslatedTile
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.5 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='center'>An array of floats representing a vector</param>
		/// <param name='angle'></param>
		/// <param name='acuteAngle'></param>
		/// <param name='width'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter FourfoldTranslatedTile(float[] center, float angle, float acuteAngle, float width) {
			return Filter("CIFourfoldTranslatedTile", new Dictionary<string, object>() {
				{"inputCenter",new CIVector("[" + string.Join(" ", Array.ConvertAll(center, x => x.ToString())) + "]")},
				{"inputAngle",angle},
				{"inputAcuteAngle",acuteAngle},
				{"inputWidth",width}
			});
		}

		/// <summary>Adjusts midtone brightness.</summary>
		/// <remarks>
		/// <p></p><b>CIGammaAdjust</b>
		/// <p class="abstract">Adjusts midtone brightness.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>power</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeScalar</code>
		///         and whose display name is Power.
		///       </p>
		///       <p>Default value: 0.75</p>
		///     
		///   
		/// 
		/// 
		///   <p></p><b>Discussion</b>
		///   <p>This filter is typically used to compensate for nonlinear effects of displays. Adjusting the gamma effectively changes the slope of the transition between black and white. It uses the following formula:</p>
		///   <p>
		///     <code>pow(s.rgb, vec3(power))</code>
		///   </p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryNonSquarePixels</code>
		/// ,
		/// <code>CICategoryInterlaced</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryColorAdjustment</code>
		/// <p></p><b>Localized Display Name</b>
		/// Gamma Adjust
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 5.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='power'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter GammaAdjust(float power) {
			return Filter("CIGammaAdjust", new Dictionary<string, object>() {
				{"inputPower",power}
			});
		}

		/// <summary>Spreads source pixels by an amount specified by a Gaussian distribution.</summary>
		/// <remarks>
		/// <p></p><b>CIGaussianBlur</b>
		/// <p class="abstract">Spreads source pixels by an amount specified by a Gaussian distribution.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>radius</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeDistance</code>
		///         and whose display name is Radius.
		///       </p>
		///       <p>Default value: 10.00</p>
		///     
		///   
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryBlur</code>
		/// <p></p><b>Localized Display Name</b>
		/// Gaussian Blur
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='radius'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter GaussianBlur(float radius) {
			return Filter("CIGaussianBlur", new Dictionary<string, object>() {
				{"inputRadius",radius}
			});
		}

		/// <summary>Generates a gradient that varies from one color to another using a Gaussian distribution.</summary>
		/// <remarks>
		/// <p></p><b>CIGaussianGradient</b>
		/// <p class="abstract">Generates a gradient that varies from one color to another using a Gaussian distribution.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>color0</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>Color3</c>
		///         object whose display name is Color 1.
		///       </p>
		///     
		///     
		///       <em>color1</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>Color3</c>
		///         object whose display name is Color 2.
		///       </p>
		///     
		///     
		///       <em>radius</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeDistance</code>
		///         and whose display name is Radius.
		///       </p>
		///       <p>Default value: 300.00</p>
		///     
		///   
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryGradient</code>
		/// <p></p><b>Localized Display Name</b>
		/// Gaussian Gradient
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='color0'>A color in RGBA format</param>
		/// <param name='color1'>A color in RGBA format</param>
		/// <param name='radius'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter GaussianGradient(Color32 color0, Color32 color1, float radius) {
			return Filter("CIGaussianGradient", new Dictionary<string, object>() {
				{"inputColor0",CIColor.FromColor32(color0)},
				{"inputColor1",CIColor.FromColor32(color1)},
				{"inputRadius",radius}
			});
		}

		/// <summary>Produces a tiled image from a source image by translating and smearing the image.</summary>
		/// <remarks>
		/// <p></p><b>CIGlideReflectedTile</b>
		/// <p class="abstract">Produces a tiled image from a source image by translating and smearing the image.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>center</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypePosition</code>
		///         and whose display name is Center.
		///       </p>
		///       <p>Default value: [150 150]</p>
		///     
		///     
		///       <em>angle</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeAngle</code>
		///         and whose display name is Angle.
		///       </p>
		///       <p>Default value: 0.00</p>
		///     
		///     
		///       <em>width</em>
		///     
		///     
		///       <p>
		///         The width, along with the
		///         <code>inputCenter</code>
		///         parameter, defines the portion of the image to tile. An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeDistance</code>
		///         and whose display name is Width.
		///       </p>
		///       <p>Default value: 100.00</p>
		///     
		///   
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryTileEffect</code>
		/// <p></p><b>Localized Display Name</b>
		/// CIGlideReflectedTile
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.5 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='center'>An array of floats representing a vector</param>
		/// <param name='angle'></param>
		/// <param name='width'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter GlideReflectedTile(float[] center, float angle, float width) {
			return Filter("CIGlideReflectedTile", new Dictionary<string, object>() {
				{"inputCenter",new CIVector("[" + string.Join(" ", Array.ConvertAll(center, x => x.ToString())) + "]")},
				{"inputAngle",angle},
				{"inputWidth",width}
			});
		}

		/// <summary>Dulls the highlights of an image.</summary>
		/// <remarks>
		/// <p></p><b>CIGloom</b>
		/// <p class="abstract">Dulls the highlights of an image.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>radius</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeDistance</code>
		///         and whose display name is Radius.
		///       </p>
		///       <p>Default value: 10.00</p>
		///     
		///     
		///       <em>intensity</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeScalar</code>
		///         and whose display name is Intensity.
		///       </p>
		///       <p>Default value: 1.00</p>
		///     
		///   
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryStylize</code>
		/// <p></p><b>Localized Display Name</b>
		/// Gloom
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='radius'></param>
		/// <param name='intensity'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter Gloom(float radius, float intensity) {
			return Filter("CIGloom", new Dictionary<string, object>() {
				{"inputRadius",radius},
				{"inputIntensity",intensity}
			});
		}

		/// <summary>Either multiplies or screens colors, depending on the source image sample color.</summary>
		/// <remarks>
		/// <p></p><b>CIHardLightBlendMode</b>
		/// <p class="abstract">Either multiplies or screens colors, depending on the source image sample color.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>backgroundImage</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>Texture2D</c>
		///         object whose display name is Background Image.
		///       </p>
		///     
		///   
		/// 
		/// 
		///   <p></p><b>Discussion</b>
		///   <p>
		///     If the source image sample color is lighter than 50% gray, the background is lightened, similar to screening. If the source image sample color is darker than 50% gray, the background is darkened, similar to multiplying. If the source image sample color is equal to 50% gray, the source image is not changed. Image samples that are equal to pure black or pure white result in pure black or white. The overall effect is similar to what you would achieve by shining a harsh spotlight on the source image. The formula used to create this filter is described in the PDF specification, which is available online from the Adobe Developer Center. See
		///     <span class="content_text">
		///       <a href="http://www.adobe.com/devnet/pdf/pdf_reference.html" class="urlLink" rel="external">PDF Reference and Adobe Extensions to the PDF Specification</a>
		///     </span>
		///     .
		///   </p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryNonSquarePixels</code>
		/// ,
		/// <code>CICategoryInterlaced</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryCompositeOperation</code>
		/// <p></p><b>Localized Display Name</b>
		/// Hard Light Blend Mode
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='backgroundImage'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter HardLightBlendMode(Texture2D backgroundImage) {
			return Filter("CIHardLightBlendMode", new Dictionary<string, object>() {
				{"inputBackgroundImage",CIImage.FromTexture2D(backgroundImage)}
			});
		}

		/// <summary>Simulates the hatched pattern of a halftone screen.</summary>
		/// <remarks>
		/// <p></p><b>CIHatchedScreen</b>
		/// <p class="abstract">Simulates the hatched pattern of a halftone screen.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>center</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypePosition</code>
		///         and whose display name is Center.
		///       </p>
		///       <p>Default value: [150 150]</p>
		///     
		///     
		///       <em>angle</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeAngle</code>
		///         and whose display name is Angle.
		///       </p>
		///       <p>Default value: 0.00</p>
		///     
		///     
		///       <em>width</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeDistance</code>
		///         and whose display name is Width.
		///       </p>
		///       <p>Default value: 6.00</p>
		///     
		///     
		///       <em>sharpness</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeScalar</code>
		///         and whose display name is Sharpness.
		///       </p>
		///       <p>Default value: 0.70</p>
		///     
		///   
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryHalftoneEffect</code>
		/// <p></p><b>Localized Display Name</b>
		/// Hatched Screen
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='center'>An array of floats representing a vector</param>
		/// <param name='angle'></param>
		/// <param name='width'></param>
		/// <param name='sharpness'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter HatchedScreen(float[] center, float angle, float width, float sharpness) {
			return Filter("CIHatchedScreen", new Dictionary<string, object>() {
				{"inputCenter",new CIVector("[" + string.Join(" ", Array.ConvertAll(center, x => x.ToString())) + "]")},
				{"inputAngle",angle},
				{"inputWidth",width},
				{"inputSharpness",sharpness}
			});
		}

		/// <summary>Adjust the tonal mapping of an image while preserving spatial detail.</summary>
		/// <remarks>
		/// <p></p><b>CIHighlightShadowAdjust</b>
		/// <p class="abstract">Adjust the tonal mapping of an image while preserving spatial detail.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>highlightAmount</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeScalar</code>
		///         and whose display name is Highlight Amount.
		///       </p>
		///       <p>Default value: 1.00</p>
		///     
		///     
		///       <em>shadowAmount</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeScalar</code>
		///         and whose display name is Shadow Amount.
		///       </p>
		///     
		///   
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryStylize</code>
		/// <p></p><b>Localized Display Name</b>
		/// Highlight and Shadows
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.7 and later and in iOS 5.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='highlightAmount'></param>
		/// <param name='shadowAmount'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter HighlightShadowAdjust(float highlightAmount, float shadowAmount) {
			return Filter("CIHighlightShadowAdjust", new Dictionary<string, object>() {
				{"inputHighlightAmount",highlightAmount},
				{"inputShadowAmount",shadowAmount}
			});
		}

		/// <summary>Creates a circular area that pushes the image pixels outward, distorting those pixels closest to the circle the most.</summary>
		/// <remarks>
		/// <p></p><b>CIHoleDistortion</b>
		/// <p class="abstract">Creates a circular area that pushes the image pixels outward, distorting those pixels closest to the circle the most.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>center</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypePosition</code>
		///         and whose display name is Center.
		///       </p>
		///       <p>Default value: [150 150]</p>
		///     
		///     
		///       <em>radius</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeDistance</code>
		///         and whose display name is Radius.
		///       </p>
		///       <p>Default value: 150.00</p>
		///     
		///   
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryDistortionEffect</code>
		/// <p></p><b>Localized Display Name</b>
		/// Hole Distortion
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='center'>An array of floats representing a vector</param>
		/// <param name='radius'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter HoleDistortion(float[] center, float radius) {
			return Filter("CIHoleDistortion", new Dictionary<string, object>() {
				{"inputCenter",new CIVector("[" + string.Join(" ", Array.ConvertAll(center, x => x.ToString())) + "]")},
				{"inputRadius",radius}
			});
		}

		/// <summary>Changes the overall hue, or tint, of the source pixels.</summary>
		/// <remarks>
		/// <p></p><b>CIHueAdjust</b>
		/// <p class="abstract">Changes the overall hue, or tint, of the source pixels.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>angle</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeAngle</code>
		///         and whose display name is Angle.
		///       </p>
		///       <p>Default value: 0.00</p>
		///     
		///   
		/// 
		/// 
		///   <p></p><b>Discussion</b>
		///   <p>This filter essentially rotates the color cube around the neutral axis.</p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryNonSquarePixels</code>
		/// ,
		/// <code>CICategoryInterlaced</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryColorAdjustment</code>
		/// <p></p><b>Localized Display Name</b>
		/// Hue Adjust
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 5.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='angle'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter HueAdjust(float angle) {
			return Filter("CIHueAdjust", new Dictionary<string, object>() {
				{"inputAngle",angle}
			});
		}

		/// <summary>Uses the luminance and saturation values of the background image with the hue of the input image.</summary>
		/// <remarks>
		/// <p></p><b>CIHueBlendMode</b>
		/// <p class="abstract">Uses the luminance and saturation values of the background image with the hue of the input image.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>backgroundImage</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>Texture2D</c>
		///         object whose display name is Background Image.
		///       </p>
		///     
		///   
		/// 
		/// 
		///   <p></p><b>Discussion</b>
		///   <p>
		///     The formula used to create this filter is described in the PDF specification, which is available online from the Adobe Developer Center. See
		///     <span class="content_text">
		///       <a href="http://www.adobe.com/devnet/pdf/pdf_reference.html" class="urlLink" rel="external">PDF Reference and Adobe Extensions to the PDF Specification</a>
		///     </span>
		///     .
		///   </p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryNonSquarePixels</code>
		/// ,
		/// <code>CICategoryInterlaced</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryCompositeOperation</code>
		/// <p></p><b>Localized Display Name</b>
		/// Hue Blend Mode
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='backgroundImage'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter HueBlendMode(Texture2D backgroundImage) {
			return Filter("CIHueBlendMode", new Dictionary<string, object>() {
				{"inputBackgroundImage",CIImage.FromTexture2D(backgroundImage)}
			});
		}

		/// <summary>Produces a high-quality, scaled version of a source image.</summary>
		/// <remarks>
		/// <p></p><b>CILanczosScaleTransform</b>
		/// <p class="abstract">Produces a high-quality, scaled version of a source image.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>scale</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeScalar</code>
		///         and whose display name is Scale.
		///       </p>
		///       <p>Default value: 1.00</p>
		///     
		///     
		///       <em>aspectRatio</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeScalar</code>
		///         and whose display name is Aspect Ratio.
		///       </p>
		///       <p>Default value: 1.00</p>
		///     
		///   
		/// 
		/// 
		///   <p></p><b>Discussion</b>
		///   <p>You typically use this filter to scale down an image.</p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryGeometryAdjustment</code>
		/// <p></p><b>Localized Display Name</b>
		/// Lanczos Scale Transform
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='scale'></param>
		/// <param name='aspectRatio'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter LanczosScaleTransform(float scale, float aspectRatio) {
			return Filter("CILanczosScaleTransform", new Dictionary<string, object>() {
				{"inputScale",scale},
				{"inputAspectRatio",aspectRatio}
			});
		}

		/// <summary>Creates composite image samples by choosing the lighter samples (either from the source image or the background).</summary>
		/// <remarks>
		/// <p></p><b>CILightenBlendMode</b>
		/// <p class="abstract">Creates composite image samples by choosing the lighter samples (either from the source image or the background).</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>backgroundImage</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>Texture2D</c>
		///         object whose display name is Background Image.
		///       </p>
		///     
		///   
		/// 
		/// 
		///   <p></p><b>Discussion</b>
		///   <p>
		///     The result is that the background image samples are replaced by any source image samples that are lighter. Otherwise, the background image samples are left unchanged. The formula used to create this filter is described in the PDF specification, which is available online from the Adobe Developer Center. See
		///     <span class="content_text">
		///       <a href="http://www.adobe.com/devnet/pdf/pdf_reference.html" class="urlLink" rel="external">PDF Reference and Adobe Extensions to the PDF Specification</a>
		///     </span>
		///     .
		///   </p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryNonSquarePixels</code>
		/// ,
		/// <code>CICategoryInterlaced</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryCompositeOperation</code>
		/// <p></p><b>Localized Display Name</b>
		/// Lighten Blend Mode
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='backgroundImage'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter LightenBlendMode(Texture2D backgroundImage) {
			return Filter("CILightenBlendMode", new Dictionary<string, object>() {
				{"inputBackgroundImage",CIImage.FromTexture2D(backgroundImage)}
			});
		}

		/// <summary>Rotates a portion of the input image specified by the center and radius parameters to give a tunneling effect.</summary>
		/// <remarks>
		/// <p></p><b>CILightTunnel</b>
		/// <p class="abstract">Rotates a portion of the input image specified by the center and radius parameters to give a tunneling effect.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>center</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypePosition</code>
		///         .
		///       </p>
		///       <p>Default value: [150 150]</p>
		///     
		///     
		///       <em>rotation</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeAngle</code>
		///         .
		///       </p>
		///       <p>Default value: 0.00</p>
		///     
		///     
		///       <em>radius</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeAngle</code>
		///         .
		///       </p>
		///       <p>Default value: 0.00</p>
		///     
		///   
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryDistortionEffect</code>
		/// <p></p><b>Localized Display Name</b>
		/// Light Tunnel
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='center'>An array of floats representing a vector</param>
		/// <param name='rotation'></param>
		/// <param name='radius'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter LightTunnel(float[] center, float rotation, float radius) {
			return Filter("CILightTunnel", new Dictionary<string, object>() {
				{"inputCenter",new CIVector("[" + string.Join(" ", Array.ConvertAll(center, x => x.ToString())) + "]")},
				{"inputRotation",rotation},
				{"inputRadius",radius}
			});
		}

		/// <summary>Generates a gradient that varies along a linear axis between two defined endpoints.</summary>
		/// <remarks>
		/// <p></p><b>CILinearGradient</b>
		/// <p class="abstract">Generates a gradient that varies along a linear axis between two defined endpoints.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>point1</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypePosition</code>
		///         and whose display name is Point 2.
		///       </p>
		///       <p>Default value: [200 200]</p>
		///     
		///     
		///       <em>color0</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>Color3</c>
		///         object whose display name is Color 1.
		///       </p>
		///     
		///     
		///       <em>color1</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>Color3</c>
		///         object whose display name is Color 2.
		///       </p>
		///     
		///   
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryGradient</code>
		/// <p></p><b>Localized Display Name</b>
		/// Linear Gradient
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='point1'>An array of floats representing a vector</param>
		/// <param name='color0'>A color in RGBA format</param>
		/// <param name='color1'>A color in RGBA format</param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter LinearGradient(float[] point1, Color32 color0, Color32 color1) {
			return Filter("CILinearGradient", new Dictionary<string, object>() {
				{"inputPoint1",new CIVector("[" + string.Join(" ", Array.ConvertAll(point1, x => x.ToString())) + "]")},
				{"inputColor0",CIColor.FromColor32(color0)},
				{"inputColor1",CIColor.FromColor32(color1)}
			});
		}

		/// <summary>Maps color intensity from a linear gamma curve to the sRGB color space.</summary>
		/// <remarks>
		/// <p></p><b>CILinearToSRGBToneCurve</b>
		/// <p class="abstract">Maps color intensity from a linear gamma curve to the sRGB color space.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryNonSquarePixels</code>
		/// ,
		/// <code>CICategoryInterlaced</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryColorAdjustment</code>
		/// <p></p><b>Localized Display Name</b>
		/// Linear to sRGB Tone Curve
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in iOS 7.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter LinearToSRGBToneCurve() {
			return Filter("CILinearToSRGBToneCurve", new Dictionary<string, object>() {
			});
		}

		/// <summary>Simulates the line pattern of a halftone screen.</summary>
		/// <remarks>
		/// <p></p><b>CILineScreen</b>
		/// <p class="abstract">Simulates the line pattern of a halftone screen.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>center</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypePosition</code>
		///         and whose display name is Center.
		///       </p>
		///       <p>Default value: [150 150]</p>
		///     
		///     
		///       <em>angle</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeAngle</code>
		///         and whose display name is Angle.
		///       </p>
		///     
		///     
		///       <em>width</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeDistance</code>
		///         and whose display name is Width.
		///       </p>
		///       <p>Default value: 6.00</p>
		///     
		///     
		///       <em>sharpness</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeScalar</code>
		///         and whose display name is Sharpness.
		///       </p>
		///       <p>Default value: 0.70</p>
		///     
		///   
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryHalftoneEffect</code>
		/// <p></p><b>Localized Display Name</b>
		/// Line Screen
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='center'>An array of floats representing a vector</param>
		/// <param name='angle'></param>
		/// <param name='width'></param>
		/// <param name='sharpness'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter LineScreen(float[] center, float angle, float width, float sharpness) {
			return Filter("CILineScreen", new Dictionary<string, object>() {
				{"inputCenter",new CIVector("[" + string.Join(" ", Array.ConvertAll(center, x => x.ToString())) + "]")},
				{"inputAngle",angle},
				{"inputWidth",width},
				{"inputSharpness",sharpness}
			});
		}

		/// <summary>Uses the hue and saturation of the background image with the luminance of the input image.</summary>
		/// <remarks>
		/// <p></p><b>CILuminosityBlendMode</b>
		/// <p class="abstract">Uses the hue and saturation of the background image with the luminance of the input image.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>backgroundImage</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>Texture2D</c>
		///         object whose display name is Background Image.
		///       </p>
		///     
		///   
		/// 
		/// 
		///   <p></p><b>Discussion</b>
		///   <p>
		///     This mode creates an effect that is inverse to the effect created by the CIColorBlendMode filter. The formula used to create this filter is described in the PDF specification, which is available online from the Adobe Developer Center. See
		///     <span class="content_text">
		///       <a href="http://www.adobe.com/devnet/pdf/pdf_reference.html" class="urlLink" rel="external">PDF Reference and Adobe Extensions to the PDF Specification</a>
		///     </span>
		///     .
		///   </p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryNonSquarePixels</code>
		/// ,
		/// <code>CICategoryInterlaced</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryCompositeOperation</code>
		/// <p></p><b>Localized Display Name</b>
		/// Luminosity Blend Mode
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='backgroundImage'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter LuminosityBlendMode(Texture2D backgroundImage) {
			return Filter("CILuminosityBlendMode", new Dictionary<string, object>() {
				{"inputBackgroundImage",CIImage.FromTexture2D(backgroundImage)}
			});
		}

		/// <summary>Converts a grayscale image to a white image that is masked by alpha.</summary>
		/// <remarks>
		/// <p></p><b>CIMaskToAlpha</b>
		/// <p class="abstract">Converts a grayscale image to a white image that is masked by alpha.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		/// 
		/// 
		///   <p></p><b>Discussion</b>
		///   <p>The white values from the source image produce the inside of the mask; the black values become completely transparent.</p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryNonSquarePixels</code>
		/// ,
		/// <code>CICategoryInterlaced</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryColorEffect</code>
		/// <p></p><b>Localized Display Name</b>
		/// Mask To Alpha
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter MaskToAlpha() {
			return Filter("CIMaskToAlpha", new Dictionary<string, object>() {
			});
		}

		/// <summary>Returns a grayscale image from max(r,g,b).</summary>
		/// <remarks>
		/// <p></p><b>CIMaximumComponent</b>
		/// <p class="abstract">Returns a grayscale image from max(r,g,b).</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryNonSquarePixels</code>
		/// ,
		/// <code>CICategoryInterlaced</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryColorEffect</code>
		/// <p></p><b>Localized Display Name</b>
		/// Maximum Component
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.5 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter MaximumComponent() {
			return Filter("CIMaximumComponent", new Dictionary<string, object>() {
			});
		}

		/// <summary>Computes the maximum value, by color component, of two input images and creates an output image using the maximum values.</summary>
		/// <remarks>
		/// <p></p><b>CIMaximumCompositing</b>
		/// <p class="abstract">Computes the maximum value, by color component, of two input images and creates an output image using the maximum values.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>backgroundImage</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>Texture2D</c>
		///         object whose display name is Background Image.
		///       </p>
		///     
		///   
		/// 
		/// 
		///   <p></p><b>Discussion</b>
		///   <p>
		///     This is similar to dodging.  The formula used to create this filter is described in  Thomas Porter and Tom Duff. 1984.
		///     <span class="content_text">
		///       <a href="http://keithp.com/~keithp/porterduff/p253-porter.pdf" class="urlLink" rel="external">Compositing Digital Images</a>
		///     </span>
		///     .
		///     <em>Computer Graphics</em>
		///     , 18 (3): 253-259.
		///   </p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryHighDynamicRange</code>
		/// ,
		/// <code>CICategoryNonSquarePixels</code>
		/// ,
		/// <code>CICategoryInterlaced</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryCompositeOperation</code>
		/// <p></p><b>Localized Display Name</b>
		/// Maximum
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='backgroundImage'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter MaximumCompositing(Texture2D backgroundImage) {
			return Filter("CIMaximumCompositing", new Dictionary<string, object>() {
				{"inputBackgroundImage",CIImage.FromTexture2D(backgroundImage)}
			});
		}

		/// <summary>Returns a grayscale image from min(r,g,b).</summary>
		/// <remarks>
		/// <p></p><b>CIMinimumComponent</b>
		/// <p class="abstract">Returns a grayscale image from min(r,g,b).</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryNonSquarePixels</code>
		/// ,
		/// <code>CICategoryInterlaced</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryColorEffect</code>
		/// <p></p><b>Localized Display Name</b>
		/// Minimum Component
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.5 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter MinimumComponent() {
			return Filter("CIMinimumComponent", new Dictionary<string, object>() {
			});
		}

		/// <summary>Computes the minimum value, by color component, of two input images and creates an output image using the minimum values.</summary>
		/// <remarks>
		/// <p></p><b>CIMinimumCompositing</b>
		/// <p class="abstract">Computes the minimum value, by color component, of two input images and creates an output image using the minimum values.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>backgroundImage</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>Texture2D</c>
		///         object whose display name is Background Image.
		///       </p>
		///     
		///   
		/// 
		/// 
		///   <p></p><b>Discussion</b>
		///   <p>
		///     This is similar to burning.  The formula used to create this filter is described in  Thomas Porter and Tom Duff. 1984.
		///     <span class="content_text">
		///       <a href="http://keithp.com/~keithp/porterduff/p253-porter.pdf" class="urlLink" rel="external">Compositing Digital Images</a>
		///     </span>
		///     .
		///     <em>Computer Graphics</em>
		///     , 18 (3): 253-259.
		///   </p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryHighDynamicRange</code>
		/// ,
		/// <code>CICategoryNonSquarePixels</code>
		/// ,
		/// <code>CICategoryInterlaced</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryCompositeOperation</code>
		/// <p></p><b>Localized Display Name</b>
		/// Minimum
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='backgroundImage'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter MinimumCompositing(Texture2D backgroundImage) {
			return Filter("CIMinimumCompositing", new Dictionary<string, object>() {
				{"inputBackgroundImage",CIImage.FromTexture2D(backgroundImage)}
			});
		}

		/// <summary>Transitions from one image to another by revealing the target image through irregularly shaped holes.</summary>
		/// <remarks>
		/// <p></p><b>CIModTransition</b>
		/// <p class="abstract">Transitions from one image to another by revealing the target image through irregularly shaped holes.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>targetImage</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>Texture2D</c>
		///         object whose display name is Target Image.
		///       </p>
		///     
		///     
		///       <em>center</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypePosition</code>
		///         and whose display name is Center.
		///       </p>
		///       <p>Default value: [150 150]</p>
		///     
		///     
		///       <em>time</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeTime</code>
		///         and whose display name is Time.
		///       </p>
		///       <p>Default value: 0.00</p>
		///     
		///     
		///       <em>angle</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeAngle</code>
		///         and whose display name is Angle.
		///       </p>
		///       <p>Default value: 2.00</p>
		///     
		///     
		///       <em>radius</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeDistance</code>
		///         and whose display name is Radius.
		///       </p>
		///       <p>Default value: 150.00</p>
		///     
		///     
		///       <em>compression</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeDistance</code>
		///         and whose display name is Compression.
		///       </p>
		///       <p>Default value: 300.00</p>
		///     
		///   
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryTransition</code>
		/// <p></p><b>Localized Display Name</b>
		/// Mod
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='targetImage'></param>
		/// <param name='center'>An array of floats representing a vector</param>
		/// <param name='time'></param>
		/// <param name='angle'></param>
		/// <param name='radius'></param>
		/// <param name='compression'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter ModTransition(Texture2D targetImage, float[] center, float time, float angle, float radius, float compression) {
			return Filter("CIModTransition", new Dictionary<string, object>() {
				{"inputTargetImage",CIImage.FromTexture2D(targetImage)},
				{"inputCenter",new CIVector("[" + string.Join(" ", Array.ConvertAll(center, x => x.ToString())) + "]")},
				{"inputTime",time},
				{"inputAngle",angle},
				{"inputRadius",radius},
				{"inputCompression",compression}
			});
		}

		/// <summary>Multiplies the input image samples with the background image samples.</summary>
		/// <remarks>
		/// <p></p><b>CIMultiplyBlendMode</b>
		/// <p class="abstract">Multiplies the input image samples with the background image samples.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>backgroundImage</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>Texture2D</c>
		///         object whose display name is Background Image.
		///       </p>
		///     
		///   
		/// 
		/// 
		///   <p></p><b>Discussion</b>
		///   <p>
		///     This results in colors that are at least as dark as either of the two contributing sample colors. The formula used to create this filter is described in the PDF specification, which is available online from the Adobe Developer Center. See
		///     <span class="content_text">
		///       <a href="http://www.adobe.com/devnet/pdf/pdf_reference.html" class="urlLink" rel="external">PDF Reference and Adobe Extensions to the PDF Specification</a>
		///     </span>
		///     .
		///   </p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryNonSquarePixels</code>
		/// ,
		/// <code>CICategoryInterlaced</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryCompositeOperation</code>
		/// <p></p><b>Localized Display Name</b>
		/// Multiply Blend Mode
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='backgroundImage'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter MultiplyBlendMode(Texture2D backgroundImage) {
			return Filter("CIMultiplyBlendMode", new Dictionary<string, object>() {
				{"inputBackgroundImage",CIImage.FromTexture2D(backgroundImage)}
			});
		}

		/// <summary>Multiplies the color component of two input images and creates an output image using the multiplied values.</summary>
		/// <remarks>
		/// <p></p><b>CIMultiplyCompositing</b>
		/// <p class="abstract">Multiplies the color component of two input images and creates an output image using the multiplied values.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>backgroundImage</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>Texture2D</c>
		///         object whose display name is Background Image.
		///       </p>
		///     
		///   
		/// 
		/// 
		///   <p></p><b>Discussion</b>
		///   <p>
		///     This filter is typically used to add a spotlight or similar lighting effect to an image.  The formula used to create this filter is described in  Thomas Porter and Tom Duff. 1984.
		///     <span class="content_text">
		///       <a href="http://keithp.com/~keithp/porterduff/p253-porter.pdf" class="urlLink" rel="external">Compositing Digital Images</a>
		///     </span>
		///     .
		///     <em>Computer Graphics</em>
		///     , 18 (3): 253-259.
		///   </p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryHighDynamicRange</code>
		/// ,
		/// <code>CICategoryNonSquarePixels</code>
		/// ,
		/// <code>CICategoryInterlaced</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryCompositeOperation</code>
		/// <p></p><b>Localized Display Name</b>
		/// Multiply
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='backgroundImage'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter MultiplyCompositing(Texture2D backgroundImage) {
			return Filter("CIMultiplyCompositing", new Dictionary<string, object>() {
				{"inputBackgroundImage",CIImage.FromTexture2D(backgroundImage)}
			});
		}

		/// <summary>Either multiplies or screens the input image samples with the background image samples, depending on the background color.</summary>
		/// <remarks>
		/// <p></p><b>CIOverlayBlendMode</b>
		/// <p class="abstract">Either multiplies or screens the input image samples with the background image samples, depending on the background color.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>backgroundImage</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>Texture2D</c>
		///         object whose display name is Background Image.
		///       </p>
		///     
		///   
		/// 
		/// 
		///   <p></p><b>Discussion</b>
		///   <p>
		///     The result is to overlay the existing image samples while preserving the highlights and shadows of the background. The background color mixes with the source image to reflect the lightness or darkness of the background. The formula used to create this filter is described in the PDF specification, which is available online from the Adobe Developer Center. See
		///     <span class="content_text">
		///       <a href="http://www.adobe.com/devnet/pdf/pdf_reference.html" class="urlLink" rel="external">PDF Reference and Adobe Extensions to the PDF Specification</a>
		///     </span>
		///     .
		///   </p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryNonSquarePixels</code>
		/// ,
		/// <code>CICategoryInterlaced</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryCompositeOperation</code>
		/// <p></p><b>Localized Display Name</b>
		/// Overlay Blend Mode
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='backgroundImage'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter OverlayBlendMode(Texture2D backgroundImage) {
			return Filter("CIOverlayBlendMode", new Dictionary<string, object>() {
				{"inputBackgroundImage",CIImage.FromTexture2D(backgroundImage)}
			});
		}

		/// <summary>Applies a perspective transform to an image and then tiles the result.</summary>
		/// <remarks>
		/// <p></p><b>CIPerspectiveTile</b>
		/// <p class="abstract">Applies a perspective transform to an image and then tiles the result.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>topLeft</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypePosition</code>
		///         and whose display name is Top Left.
		///       </p>
		///       <p>Default value: [118 484]</p>
		///     
		///     
		///       <em>topRight</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypePosition</code>
		///         and whose display name is Top Right.
		///       </p>
		///       <p>Default value: [646 507]</p>
		///     
		///     
		///       <em>bottomRight</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypePosition</code>
		///         and whose display name is Bottom Right.
		///       </p>
		///       <p>Default value: [548 140]</p>
		///     
		///     
		///       <em>bottomLeft</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypePosition</code>
		///         and whose display name is Bottom Left.
		///       </p>
		///       <p>Default value: [155 153]</p>
		///     
		///   
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryTileEffect</code>
		/// <p></p><b>Localized Display Name</b>
		/// Perspective Tile
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='topLeft'>An array of floats representing a vector</param>
		/// <param name='topRight'>An array of floats representing a vector</param>
		/// <param name='bottomRight'>An array of floats representing a vector</param>
		/// <param name='bottomLeft'>An array of floats representing a vector</param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter PerspectiveTile(float[] topLeft, float[] topRight, float[] bottomRight, float[] bottomLeft) {
			return Filter("CIPerspectiveTile", new Dictionary<string, object>() {
				{"inputTopLeft",new CIVector("[" + string.Join(" ", Array.ConvertAll(topLeft, x => x.ToString())) + "]")},
				{"inputTopRight",new CIVector("[" + string.Join(" ", Array.ConvertAll(topRight, x => x.ToString())) + "]")},
				{"inputBottomRight",new CIVector("[" + string.Join(" ", Array.ConvertAll(bottomRight, x => x.ToString())) + "]")},
				{"inputBottomLeft",new CIVector("[" + string.Join(" ", Array.ConvertAll(bottomLeft, x => x.ToString())) + "]")}
			});
		}

		/// <summary>Alters the geometry of an image to simulate the observer changing viewing position.</summary>
		/// <remarks>
		/// <p></p><b>CIPerspectiveTransform</b>
		/// <p class="abstract">Alters the geometry of an image to simulate the observer changing viewing position.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>topLeft</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypePosition</code>
		///         and whose display name is Top Left.
		///       </p>
		///       <p>Default value: [118 484]</p>
		///     
		///     
		///       <em>topRight</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypePosition</code>
		///         and whose display name is Top Right.
		///       </p>
		///       <p>Default value: [646 507]</p>
		///     
		///     
		///       <em>bottomRight</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypePosition</code>
		///         and whose display name is Bottom Right.
		///       </p>
		///       <p>Default value: [548 140]</p>
		///     
		///     
		///       <em>bottomLeft</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypePosition</code>
		///         and whose display name is Bottom Left.
		///       </p>
		///       <p>Default value: [155 153]</p>
		///     
		///   
		/// 
		/// 
		///   <p></p><b>Discussion</b>
		///   <p>You can use the perspective filter to skew an image.</p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryGeometryAdjustment</code>
		/// <p></p><b>Localized Display Name</b>
		/// Perspective Transform
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='topLeft'>An array of floats representing a vector</param>
		/// <param name='topRight'>An array of floats representing a vector</param>
		/// <param name='bottomRight'>An array of floats representing a vector</param>
		/// <param name='bottomLeft'>An array of floats representing a vector</param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter PerspectiveTransform(float[] topLeft, float[] topRight, float[] bottomRight, float[] bottomLeft) {
			return Filter("CIPerspectiveTransform", new Dictionary<string, object>() {
				{"inputTopLeft",new CIVector("[" + string.Join(" ", Array.ConvertAll(topLeft, x => x.ToString())) + "]")},
				{"inputTopRight",new CIVector("[" + string.Join(" ", Array.ConvertAll(topRight, x => x.ToString())) + "]")},
				{"inputBottomRight",new CIVector("[" + string.Join(" ", Array.ConvertAll(bottomRight, x => x.ToString())) + "]")},
				{"inputBottomLeft",new CIVector("[" + string.Join(" ", Array.ConvertAll(bottomLeft, x => x.ToString())) + "]")}
			});
		}

		/// <summary>Alters the geometry of a portion of an image to simulate the observer changing viewing position.</summary>
		/// <remarks>
		/// <p></p><b>CIPerspectiveTransformWithExtent</b>
		/// <p class="abstract">Alters the geometry of a portion of an image to simulate the observer changing viewing position.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>extent</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose whose attribute type is
		///         <code>CIAttributeTypeRectangle</code>
		///         . If you pass [image extent] you’ll get the same result as using the
		///         <code>
		///           <a href="#//apple_ref/doc/filter/ci/CIPerspectiveTransform">CIPerspectiveTransform</a>
		///         </code>
		///         filter.
		///       </p>
		///     
		///     
		///       <em>topLeft</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypePosition</code>
		///         and whose display name is Top Left.
		///       </p>
		///       <p>Default value: [118 484]</p>
		///     
		///     
		///       <em>topRight</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypePosition</code>
		///         and whose display name is Top Right.
		///       </p>
		///       <p>Default value: [646 507]</p>
		///     
		///     
		///       <em>bottomRight</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypePosition</code>
		///         and whose display name is Bottom Right.
		///       </p>
		///       <p>Default value: [548 140]</p>
		///     
		///     
		///       <em>bottomLeft</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypePosition</code>
		///         and whose display name is Bottom Left.
		///       </p>
		///       <p>Default value: [155 153]</p>
		///     
		///   
		/// 
		/// 
		///   <p></p><b>Discussion</b>
		///   <p>
		///     You can use the perspective filter to skew an the portion of the image defined by extent. See
		///     <code>
		///       <a href="#//apple_ref/doc/filter/ci/CIPerspectiveTransform">CIPerspectiveTransform</a>
		///     </code>
		///     for an example of the output of this filter when you supply the input image size as the extent.
		///   </p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryGeometryAdjustment</code>
		/// <p></p><b>Localized Display Name</b>
		/// Perspective Transform With Extent
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='extent'>An array of floats representing a vector</param>
		/// <param name='topLeft'>An array of floats representing a vector</param>
		/// <param name='topRight'>An array of floats representing a vector</param>
		/// <param name='bottomRight'>An array of floats representing a vector</param>
		/// <param name='bottomLeft'>An array of floats representing a vector</param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter PerspectiveTransformWithExtent(float[] extent, float[] topLeft, float[] topRight, float[] bottomRight, float[] bottomLeft) {
			return Filter("CIPerspectiveTransformWithExtent", new Dictionary<string, object>() {
				{"inputExtent",new CIVector("[" + string.Join(" ", Array.ConvertAll(extent, x => x.ToString())) + "]")},
				{"inputTopLeft",new CIVector("[" + string.Join(" ", Array.ConvertAll(topLeft, x => x.ToString())) + "]")},
				{"inputTopRight",new CIVector("[" + string.Join(" ", Array.ConvertAll(topRight, x => x.ToString())) + "]")},
				{"inputBottomRight",new CIVector("[" + string.Join(" ", Array.ConvertAll(bottomRight, x => x.ToString())) + "]")},
				{"inputBottomLeft",new CIVector("[" + string.Join(" ", Array.ConvertAll(bottomLeft, x => x.ToString())) + "]")}
			});
		}

		/// <summary>Applies a preconfigured set of effects that imitate vintage photography film with exaggerated color.</summary>
		/// <remarks>
		/// <p></p><b>CIPhotoEffectChrome</b>
		/// <p class="abstract">Applies a preconfigured set of effects that imitate vintage photography film with exaggerated color.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryXMPSerializable</code>
		/// ,
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryNonSquarePixels</code>
		/// ,
		/// <code>CICategoryInterlaced</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryColorEffect</code>
		/// <p></p><b>Localized Display Name</b>
		/// Photo Effect Chrome
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.9 and later and in iOS 7.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter PhotoEffectChrome() {
			return Filter("CIPhotoEffectChrome", new Dictionary<string, object>() {
			});
		}

		/// <summary>Applies a preconfigured set of effects that imitate vintage photography film with diminished color.</summary>
		/// <remarks>
		/// <p></p><b>CIPhotoEffectFade</b>
		/// <p class="abstract">Applies a preconfigured set of effects that imitate vintage photography film with diminished color.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryXMPSerializable</code>
		/// ,
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryNonSquarePixels</code>
		/// ,
		/// <code>CICategoryInterlaced</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryColorEffect</code>
		/// <p></p><b>Localized Display Name</b>
		/// Photo Effect Fade
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.9 and later and in iOS 7.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter PhotoEffectFade() {
			return Filter("CIPhotoEffectFade", new Dictionary<string, object>() {
			});
		}

		/// <summary>Applies a preconfigured set of effects that imitate vintage photography film with distorted colors.</summary>
		/// <remarks>
		/// <p></p><b>CIPhotoEffectInstant</b>
		/// <p class="abstract">Applies a preconfigured set of effects that imitate vintage photography film with distorted colors.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryXMPSerializable</code>
		/// ,
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryNonSquarePixels</code>
		/// ,
		/// <code>CICategoryInterlaced</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryColorEffect</code>
		/// <p></p><b>Localized Display Name</b>
		/// Photo Effect Instant
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.9 and later and in iOS 7.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter PhotoEffectInstant() {
			return Filter("CIPhotoEffectInstant", new Dictionary<string, object>() {
			});
		}

		/// <summary>Applies a preconfigured set of effects that imitate black-and-white photography film with low contrast.</summary>
		/// <remarks>
		/// <p></p><b>CIPhotoEffectMono</b>
		/// <p class="abstract">Applies a preconfigured set of effects that imitate black-and-white photography film with low contrast.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryXMPSerializable</code>
		/// ,
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryNonSquarePixels</code>
		/// ,
		/// <code>CICategoryInterlaced</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryColorEffect</code>
		/// <p></p><b>Localized Display Name</b>
		/// Photo Effect Mono
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.9 and later and in iOS 7.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter PhotoEffectMono() {
			return Filter("CIPhotoEffectMono", new Dictionary<string, object>() {
			});
		}

		/// <summary>Applies a preconfigured set of effects that imitate black-and-white photography film with exaggerated contrast.</summary>
		/// <remarks>
		/// <p></p><b>CIPhotoEffectNoir</b>
		/// <p class="abstract">Applies a preconfigured set of effects that imitate black-and-white photography film with exaggerated contrast.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryXMPSerializable</code>
		/// ,
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryNonSquarePixels</code>
		/// ,
		/// <code>CICategoryInterlaced</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryColorEffect</code>
		/// <p></p><b>Localized Display Name</b>
		/// Photo Effect Noir
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.9 and later and in iOS 7.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter PhotoEffectNoir() {
			return Filter("CIPhotoEffectNoir", new Dictionary<string, object>() {
			});
		}

		/// <summary>Applies a preconfigured set of effects that imitate vintage photography film with emphasized cool colors.</summary>
		/// <remarks>
		/// <p></p><b>CIPhotoEffectProcess</b>
		/// <p class="abstract">Applies a preconfigured set of effects that imitate vintage photography film with emphasized cool colors.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryXMPSerializable</code>
		/// ,
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryNonSquarePixels</code>
		/// ,
		/// <code>CICategoryInterlaced</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryColorEffect</code>
		/// <p></p><b>Localized Display Name</b>
		/// Photo Effect Process
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.9 and later and in iOS 7.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter PhotoEffectProcess() {
			return Filter("CIPhotoEffectProcess", new Dictionary<string, object>() {
			});
		}

		/// <summary>Applies a preconfigured set of effects that imitate black-and-white photography film without significantly altering contrast.</summary>
		/// <remarks>
		/// <p></p><b>CIPhotoEffectTonal</b>
		/// <p class="abstract">Applies a preconfigured set of effects that imitate black-and-white photography film without significantly altering contrast.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryXMPSerializable</code>
		/// ,
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryNonSquarePixels</code>
		/// ,
		/// <code>CICategoryInterlaced</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryColorEffect</code>
		/// <p></p><b>Localized Display Name</b>
		/// Photo Effect Tonal
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.9 and later and in iOS 7.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter PhotoEffectTonal() {
			return Filter("CIPhotoEffectTonal", new Dictionary<string, object>() {
			});
		}

		/// <summary>Applies a preconfigured set of effects that imitate vintage photography film with emphasized warm colors.</summary>
		/// <remarks>
		/// <p></p><b>CIPhotoEffectTransfer</b>
		/// <p class="abstract">Applies a preconfigured set of effects that imitate vintage photography film with emphasized warm colors.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryXMPSerializable</code>
		/// ,
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryNonSquarePixels</code>
		/// ,
		/// <code>CICategoryInterlaced</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryColorEffect</code>
		/// <p></p><b>Localized Display Name</b>
		/// Photo Effect Transfer
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.9 and later and in iOS 7.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter PhotoEffectTransfer() {
			return Filter("CIPhotoEffectTransfer", new Dictionary<string, object>() {
			});
		}

		/// <summary>Creates a rectangular area that pinches source pixels inward, distorting those pixels closest to the rectangle the most.</summary>
		/// <remarks>
		/// <p></p><b>CIPinchDistortion</b>
		/// <p class="abstract">Creates a rectangular area that pinches source pixels inward, distorting those pixels closest to the rectangle the most.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>center</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypePosition</code>
		///         and whose display name is Center.
		///       </p>
		///       <p>Default value: [150 150]</p>
		///     
		///     
		///       <em>radius</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeDistance</code>
		///         and whose display name is Radius.
		///       </p>
		///       <p>Default value: 300.00</p>
		///     
		///     
		///       <em>scale</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeScalar</code>
		///         and whose display name is Scale.
		///       </p>
		///       <p>Default value: 0.50</p>
		///     
		///   
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryDistortionEffect</code>
		/// <p></p><b>Localized Display Name</b>
		/// Pinch Distortion
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='center'>An array of floats representing a vector</param>
		/// <param name='radius'></param>
		/// <param name='scale'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter PinchDistortion(float[] center, float radius, float scale) {
			return Filter("CIPinchDistortion", new Dictionary<string, object>() {
				{"inputCenter",new CIVector("[" + string.Join(" ", Array.ConvertAll(center, x => x.ToString())) + "]")},
				{"inputRadius",radius},
				{"inputScale",scale}
			});
		}

		/// <summary>Makes an image blocky by mapping the image to colored squares whose color is defined by the replaced pixels.</summary>
		/// <remarks>
		/// <p></p><b>CIPixellate</b>
		/// <p class="abstract">Makes an image blocky by mapping the image to colored squares whose color is defined by the replaced pixels.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>center</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypePosition</code>
		///         and whose display name is Center.
		///       </p>
		///       <p>Default value: [150 150]</p>
		///     
		///     
		///       <em>scale</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeDistance</code>
		///         and whose display name is Scale.
		///       </p>
		///       <p>Default value: 8.00</p>
		///     
		///   
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryStylize</code>
		/// <p></p><b>Localized Display Name</b>
		/// Pixellate
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='center'>An array of floats representing a vector</param>
		/// <param name='scale'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter Pixellate(float[] center, float scale) {
			return Filter("CIPixellate", new Dictionary<string, object>() {
				{"inputCenter",new CIVector("[" + string.Join(" ", Array.ConvertAll(center, x => x.ToString())) + "]")},
				{"inputScale",scale}
			});
		}

		/// <summary>Generates a Quick Response code (two-dimensional barcode) from input data.</summary>
		/// <remarks>
		/// <p></p><b>CIQRCodeGenerator</b>
		/// <p class="abstract">Generates a Quick Response code (two-dimensional barcode) from input data.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>correctionLevel</em>
		///     
		///     
		///       <p>
		///         A single letter specifying the error correction format. An
		///         <c>string</c>
		///         object whose display name is CorrectionLevel.
		///       </p>
		///       <p>
		///         Default value:
		///         <code>M</code>
		///       </p>
		///     
		///   
		/// 
		/// 
		///   <p></p><b>Discussion</b>
		///   <p>
		///     Generates an output image representing the input data according to the ISO/IEC 18004:2006 standard. The width and height of each module (square dot) of the code in the output image is one point. To create a QR code from a string or URL, convert it to an
		///     <code>
		///       <a href="../../../../Cocoa/Reference/Foundation/Classes/NSData_Class/Reference/Reference.html#//apple_ref/occ/cl/NSData" target="_self">NSData</a>
		///     </code>
		///     object using the
		///     <code>
		///       <a href="../../../../Cocoa/Reference/Foundation/Classes/NSString_Class/Reference/NSString.html#//apple_ref/c/econst/NSISOLatin1StringEncoding" target="_self">NSISOLatin1StringEncoding</a>
		///     </code>
		///     string encoding.
		///   </p>
		///   <p>
		///     The
		///     <em>inputCorrectionLevel</em>
		///     parameter controls the amount of additional data encoded in the output image to provide error correction. Higher levels of error correction result in larger output images but allow larger areas of the code to be damaged or obscured without. There are four possible correction modes (with corresponding error resilience levels):
		///   </p>
		///   <ul class="simple">
		///     <li>
		///       <p>
		///         <code>L</code>
		///         : 7%
		///       </p>
		///     </li>
		///     <li>
		///       <p>
		///         <code>M</code>
		///         : 15%
		///       </p>
		///     </li>
		///     <li>
		///       <p>
		///         <code>Q</code>
		///         : 25%
		///       </p>
		///     </li>
		///     <li>
		///       <p>
		///         <code>H</code>
		///         : 30%
		///       </p>
		///     </li>
		///   </ul>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryGenerator</code>
		/// <p></p><b>Localized Display Name</b>
		/// CIQRCodeGenerator
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.9 and later and in iOS 7.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='correctionLevel'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter QRCodeGenerator(string correctionLevel) {
			return Filter("CIQRCodeGenerator", new Dictionary<string, object>() {
				{"inputCorrectionLevel",correctionLevel}
			});
		}

		/// <summary>Generates a gradient that varies radially between two circles having the same center.</summary>
		/// <remarks>
		/// <p></p><b>CIRadialGradient</b>
		/// <p class="abstract">Generates a gradient that varies radially between two circles having the same center.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>radius0</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeDistance</code>
		///         and whose display name is Radius 1.
		///       </p>
		///       <p>Default value: 5.00</p>
		///     
		///     
		///       <em>radius1</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeDistance</code>
		///         and whose display name is Radius 2.
		///       </p>
		///       <p>Default value: 100.00</p>
		///     
		///     
		///       <em>color0</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>Color3</c>
		///         object whose display name is Color 1.
		///       </p>
		///     
		///     
		///       <em>color1</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>Color3</c>
		///         object whose display name is Color 2.
		///       </p>
		///     
		///   
		/// 
		/// 
		///   <p></p><b>Discussion</b>
		///   <p>It is valid for one of the two circles to have a radius of 0.</p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryGradient</code>
		/// <p></p><b>Localized Display Name</b>
		/// Radial Gradient
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='radius0'></param>
		/// <param name='radius1'></param>
		/// <param name='color0'>A color in RGBA format</param>
		/// <param name='color1'>A color in RGBA format</param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter RadialGradient(float radius0, float radius1, Color32 color0, Color32 color1) {
			return Filter("CIRadialGradient", new Dictionary<string, object>() {
				{"inputRadius0",radius0},
				{"inputRadius1",radius1},
				{"inputColor0",CIColor.FromColor32(color0)},
				{"inputColor1",CIColor.FromColor32(color1)}
			});
		}

		/// <summary>Generates an image of infinite extent whose pixel values are made up of four independent, uniformly-distributed random numbers in the 0 to 1 range.</summary>
		/// <remarks>
		/// <p></p><b>CIRandomGenerator</b>
		/// <p class="abstract">Generates an image of infinite extent whose pixel values are made up of four independent, uniformly-distributed random numbers in the 0 to 1 range.</p>
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryGenerator</code>
		/// <p></p><b>Localized Display Name</b>
		/// Random Generator
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter RandomGenerator() {
			return Filter("CIRandomGenerator", new Dictionary<string, object>() {
			});
		}

		/// <summary>Uses the luminance and hue values of the background image with the saturation of the input image.</summary>
		/// <remarks>
		/// <p></p><b>CISaturationBlendMode</b>
		/// <p class="abstract">Uses the luminance and hue values of the background image with the saturation of the input image.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>backgroundImage</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>Texture2D</c>
		///         object whose display name is Background Image.
		///       </p>
		///     
		///   
		/// 
		/// 
		///   <p></p><b>Discussion</b>
		///   <p>
		///     Areas of the background that have no saturation (that is, pure gray areas) do not produce a change. The formula used to create this filter is described in the PDF specification, which is available online from the Adobe Developer Center. See
		///     <span class="content_text">
		///       <a href="http://www.adobe.com/devnet/pdf/pdf_reference.html" class="urlLink" rel="external">PDF Reference and Adobe Extensions to the PDF Specification</a>
		///     </span>
		///     .
		///   </p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryNonSquarePixels</code>
		/// ,
		/// <code>CICategoryInterlaced</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryCompositeOperation</code>
		/// <p></p><b>Localized Display Name</b>
		/// Saturation Blend Mode
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='backgroundImage'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter SaturationBlendMode(Texture2D backgroundImage) {
			return Filter("CISaturationBlendMode", new Dictionary<string, object>() {
				{"inputBackgroundImage",CIImage.FromTexture2D(backgroundImage)}
			});
		}

		/// <summary>Multiplies the inverse of the input image samples with the inverse of the background image samples.</summary>
		/// <remarks>
		/// <p></p><b>CIScreenBlendMode</b>
		/// <p class="abstract">Multiplies the inverse of the input image samples with the inverse of the background image samples.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>backgroundImage</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>Texture2D</c>
		///         object whose display name is Background Image.
		///       </p>
		///     
		///   
		/// 
		/// 
		///   <p></p><b>Discussion</b>
		///   <p>
		///     This results in colors that are at least as light as either of the two contributing sample colors. The formula used to create this filter is described in the PDF specification, which is available online from the Adobe Developer Center. See
		///     <span class="content_text">
		///       <a href="http://www.adobe.com/devnet/pdf/pdf_reference.html" class="urlLink" rel="external">PDF Reference and Adobe Extensions to the PDF Specification</a>
		///     </span>
		///     .
		///   </p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryNonSquarePixels</code>
		/// ,
		/// <code>CICategoryInterlaced</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryCompositeOperation</code>
		/// <p></p><b>Localized Display Name</b>
		/// Screen Blend Mode
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='backgroundImage'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter ScreenBlendMode(Texture2D backgroundImage) {
			return Filter("CIScreenBlendMode", new Dictionary<string, object>() {
				{"inputBackgroundImage",CIImage.FromTexture2D(backgroundImage)}
			});
		}

		/// <summary>Maps the colors of an image to various shades of brown.</summary>
		/// <remarks>
		/// <p></p><b>CISepiaTone</b>
		/// <p class="abstract">Maps the colors of an image to various shades of brown.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>intensity</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeScalar</code>
		///         and whose display name is Intensity.
		///       </p>
		///       <p>Default value: 1.00</p>
		///     
		///   
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryNonSquarePixels</code>
		/// ,
		/// <code>CICategoryInterlaced</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryColorEffect</code>
		/// <p></p><b>Localized Display Name</b>
		/// Sepia Tone
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 5.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='intensity'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter SepiaTone(float intensity) {
			return Filter("CISepiaTone", new Dictionary<string, object>() {
				{"inputIntensity",intensity}
			});
		}

		/// <summary>Increases image detail by sharpening.</summary>
		/// <remarks>
		/// <p></p><b>CISharpenLuminance</b>
		/// <p class="abstract">Increases image detail by sharpening.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>sharpness</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeScalar</code>
		///         and whose display name is Sharpness.
		///       </p>
		///       <p>Default value: 0.40</p>
		///     
		///   
		/// 
		/// 
		///   <p></p><b>Discussion</b>
		///   <p>It operates on the luminance of the image; the chrominance of the pixels remains unaffected.</p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategorySharpen</code>
		/// <p></p><b>Localized Display Name</b>
		/// Sharpen Luminance
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='sharpness'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter SharpenLuminance(float sharpness) {
			return Filter("CISharpenLuminance", new Dictionary<string, object>() {
				{"inputSharpness",sharpness}
			});
		}

		/// <summary>Produces a tiled image from a source image by applying a 6-way reflected symmetry.</summary>
		/// <remarks>
		/// <p></p><b>CISixfoldReflectedTile</b>
		/// <p class="abstract">Produces a tiled image from a source image by applying a 6-way reflected symmetry.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>center</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypePosition</code>
		///         and whose display name is Center.
		///       </p>
		///       <p>Default value: [150 150]</p>
		///     
		///     
		///       <em>angle</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeAngle</code>
		///         and whose display name is Angle.
		///       </p>
		///       <p>Default value: 0.00</p>
		///     
		///     
		///       <em>width</em>
		///     
		///     
		///       <p>
		///         The width, along with the
		///         <code>inputCenter</code>
		///         parameter, defines the portion of the image to tile. An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeDistance</code>
		///         and whose display name is Width.
		///       </p>
		///       <p>Default value: 100.00</p>
		///     
		///   
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryTileEffect</code>
		/// <p></p><b>Localized Display Name</b>
		/// CISixfoldReflectedTile
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.5 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='center'>An array of floats representing a vector</param>
		/// <param name='angle'></param>
		/// <param name='width'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter SixfoldReflectedTile(float[] center, float angle, float width) {
			return Filter("CISixfoldReflectedTile", new Dictionary<string, object>() {
				{"inputCenter",new CIVector("[" + string.Join(" ", Array.ConvertAll(center, x => x.ToString())) + "]")},
				{"inputAngle",angle},
				{"inputWidth",width}
			});
		}

		/// <summary>Produces a tiled image from a source image by rotating the source image at increments of 60 degrees.</summary>
		/// <remarks>
		/// <p></p><b>CISixfoldRotatedTile</b>
		/// <p class="abstract">Produces a tiled image from a source image by rotating the source image at increments of 60 degrees.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>center</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypePosition</code>
		///         and whose display name is Center.
		///       </p>
		///       <p>Default value: [150 150]</p>
		///     
		///     
		///       <em>angle</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeAngle</code>
		///         and whose display name is Angle.
		///       </p>
		///       <p>Default value: 0.00</p>
		///     
		///     
		///       <em>width</em>
		///     
		///     
		///       <p>
		///         The width, along with the
		///         <code>inputCenter</code>
		///         parameter, defines the portion of the image to tile. An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeDistance</code>
		///         and whose display name is Width.
		///       </p>
		///       <p>Default value: 100.00</p>
		///     
		///   
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryTileEffect</code>
		/// <p></p><b>Localized Display Name</b>
		/// CISixfoldRotatedTile
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.5 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='center'>An array of floats representing a vector</param>
		/// <param name='angle'></param>
		/// <param name='width'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter SixfoldRotatedTile(float[] center, float angle, float width) {
			return Filter("CISixfoldRotatedTile", new Dictionary<string, object>() {
				{"inputCenter",new CIVector("[" + string.Join(" ", Array.ConvertAll(center, x => x.ToString())) + "]")},
				{"inputAngle",angle},
				{"inputWidth",width}
			});
		}

		/// <summary>Generates a gradient that uses an S-curve function to blend colors along a linear axis between two defined endpoints.</summary>
		/// <remarks>
		/// <p></p><b>CISmoothLinearGradient</b>
		/// <p class="abstract">Generates a gradient that uses an S-curve function to blend colors along a linear axis between two defined endpoints.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>point1</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypePosition</code>
		///         and whose display name is Point 2.
		///       </p>
		///       <p>Default value: [200 200]</p>
		///     
		///     
		///       <em>color0</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>Color3</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeColor</code>
		///         and whose display name is Color 1.
		///       </p>
		///     
		///     
		///       <em>color1</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>Color3</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeColor</code>
		///         and whose display name is Color 2.
		///       </p>
		///     
		///   
		/// 
		/// 
		///   <p></p><b>Discussion</b>
		///   <p>Where the CILinearGradient filter blends colors linearly (that is, the color at a point 25% along the line between Point 1 and Point 2 is 25% Color 1 and 75% Color 2), this filter blends colors using an S-curve function: the color blend at points less than 50% along the line between Point 1 and Point 2 is slightly closer to Color 1 than in a linear blend, and the color blend at points further than 50% along that line is slightly closer to Color 2 than in a linear blend.</p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryGradient</code>
		/// <p></p><b>Localized Display Name</b>
		/// Smooth Linear Gradient
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in iOS 7.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='point1'>An array of floats representing a vector</param>
		/// <param name='color0'>A color in RGBA format</param>
		/// <param name='color1'>A color in RGBA format</param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter SmoothLinearGradient(float[] point1, Color32 color0, Color32 color1) {
			return Filter("CISmoothLinearGradient", new Dictionary<string, object>() {
				{"inputPoint1",new CIVector("[" + string.Join(" ", Array.ConvertAll(point1, x => x.ToString())) + "]")},
				{"inputColor0",CIColor.FromColor32(color0)},
				{"inputColor1",CIColor.FromColor32(color1)}
			});
		}

		/// <summary>Either darkens or lightens colors, depending on the input image sample color.</summary>
		/// <remarks>
		/// <p></p><b>CISoftLightBlendMode</b>
		/// <p class="abstract">Either darkens or lightens colors, depending on the input image sample color.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>backgroundImage</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>Texture2D</c>
		///         object whose display name is Background Image.
		///       </p>
		///     
		///   
		/// 
		/// 
		///   <p></p><b>Discussion</b>
		///   <p>
		///     If the source image sample color is lighter than 50% gray, the background is lightened, similar to dodging. If the source image sample color is darker than 50% gray, the background is darkened, similar to burning. If the source image sample color is equal to 50% gray, the background is not changed. Image samples that are equal to pure black or pure white produce darker or lighter areas, but do not result in pure black or white. The overall effect is similar to what you would achieve by shining a diffuse spotlight on the source image. The formula used to create this filter is described in the PDF specification, which is available online from the Adobe Developer Center. See
		///     <span class="content_text">
		///       <a href="http://www.adobe.com/devnet/pdf/pdf_reference.html" class="urlLink" rel="external">PDF Reference and Adobe Extensions to the PDF Specification</a>
		///     </span>
		///     .
		///   </p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryNonSquarePixels</code>
		/// ,
		/// <code>CICategoryInterlaced</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryCompositeOperation</code>
		/// <p></p><b>Localized Display Name</b>
		/// Soft Light Blend Mode
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='backgroundImage'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter SoftLightBlendMode(Texture2D backgroundImage) {
			return Filter("CISoftLightBlendMode", new Dictionary<string, object>() {
				{"inputBackgroundImage",CIImage.FromTexture2D(backgroundImage)}
			});
		}

		/// <summary>Places the input image over the background image, then uses the luminance of the background image to determine what to show.</summary>
		/// <remarks>
		/// <p></p><b>CISourceAtopCompositing</b>
		/// <p class="abstract">Places the input image over the background image, then uses the luminance of the background image to determine what to show.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>backgroundImage</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>Texture2D</c>
		///         object whose display name is Background Image.
		///       </p>
		///     
		///   
		/// 
		/// 
		///   <p></p><b>Discussion</b>
		///   <p>
		///     The composite shows the background image and only those portions of the source image that are over visible parts of the background.  The formula used to create this filter is described in  Thomas Porter and Tom Duff. 1984.
		///     <span class="content_text">
		///       <a href="http://keithp.com/~keithp/porterduff/p253-porter.pdf" class="urlLink" rel="external">Compositing Digital Images</a>
		///     </span>
		///     .
		///     <em>Computer Graphics</em>
		///     , 18 (3): 253-259.
		///   </p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryHighDynamicRange</code>
		/// ,
		/// <code>CICategoryNonSquarePixels</code>
		/// ,
		/// <code>CICategoryInterlaced</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryCompositeOperation</code>
		/// <p></p><b>Localized Display Name</b>
		/// Source Atop
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='backgroundImage'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter SourceAtopCompositing(Texture2D backgroundImage) {
			return Filter("CISourceAtopCompositing", new Dictionary<string, object>() {
				{"inputBackgroundImage",CIImage.FromTexture2D(backgroundImage)}
			});
		}

		/// <summary>Uses the background image to define what to leave in the input image, effectively cropping the input image.</summary>
		/// <remarks>
		/// <p></p><b>CISourceInCompositing</b>
		/// <p class="abstract">Uses the background image to define what to leave in the input image, effectively cropping the input image.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>backgroundImage</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>Texture2D</c>
		///         object whose display name is Background Image.
		///       </p>
		///     
		///   
		/// 
		/// 
		///   <p></p><b>Discussion</b>
		///   <p>
		///     The formula used to create this filter is described in  Thomas Porter and Tom Duff. 1984.
		///     <span class="content_text">
		///       <a href="http://keithp.com/~keithp/porterduff/p253-porter.pdf" class="urlLink" rel="external">Compositing Digital Images</a>
		///     </span>
		///     .
		///     <em>Computer Graphics</em>
		///     , 18 (3): 253-259.
		///   </p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryHighDynamicRange</code>
		/// ,
		/// <code>CICategoryNonSquarePixels</code>
		/// ,
		/// <code>CICategoryInterlaced</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryCompositeOperation</code>
		/// <p></p><b>Localized Display Name</b>
		/// Source In
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='backgroundImage'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter SourceInCompositing(Texture2D backgroundImage) {
			return Filter("CISourceInCompositing", new Dictionary<string, object>() {
				{"inputBackgroundImage",CIImage.FromTexture2D(backgroundImage)}
			});
		}

		/// <summary>Uses the background image to define what to take out of the input image.</summary>
		/// <remarks>
		/// <p></p><b>CISourceOutCompositing</b>
		/// <p class="abstract">Uses the background image to define what to take out of the input image.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>backgroundImage</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>Texture2D</c>
		///         object whose display name is Background Image.
		///       </p>
		///     
		///   
		/// 
		/// 
		///   <p></p><b>Discussion</b>
		///   <p>
		///     The formula used to create this filter is described in  Thomas Porter and Tom Duff. 1984.
		///     <span class="content_text">
		///       <a href="http://keithp.com/~keithp/porterduff/p253-porter.pdf" class="urlLink" rel="external">Compositing Digital Images</a>
		///     </span>
		///     .
		///     <em>Computer Graphics</em>
		///     , 18 (3): 253-259.
		///   </p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryHighDynamicRange</code>
		/// ,
		/// <code>CICategoryNonSquarePixels</code>
		/// ,
		/// <code>CICategoryInterlaced</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryCompositeOperation</code>
		/// <p></p><b>Localized Display Name</b>
		/// Source Out
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='backgroundImage'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter SourceOutCompositing(Texture2D backgroundImage) {
			return Filter("CISourceOutCompositing", new Dictionary<string, object>() {
				{"inputBackgroundImage",CIImage.FromTexture2D(backgroundImage)}
			});
		}

		/// <summary>Places the input image over the input background image.</summary>
		/// <remarks>
		/// <p></p><b>CISourceOverCompositing</b>
		/// <p class="abstract">Places the input image over the input background image.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>backgroundImage</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>Texture2D</c>
		///         object whose display name is Background Image.
		///       </p>
		///     
		///   
		/// 
		/// 
		///   <p></p><b>Discussion</b>
		///   <p>
		///     The formula used to create this filter is described in  Thomas Porter and Tom Duff. 1984.
		///     <span class="content_text">
		///       <a href="http://keithp.com/~keithp/porterduff/p253-porter.pdf" class="urlLink" rel="external">Compositing Digital Images</a>
		///     </span>
		///     .
		///     <em>Computer Graphics</em>
		///     , 18 (3): 253-259.
		///   </p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryHighDynamicRange</code>
		/// ,
		/// <code>CICategoryNonSquarePixels</code>
		/// ,
		/// <code>CICategoryInterlaced</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryCompositeOperation</code>
		/// <p></p><b>Localized Display Name</b>
		/// Source Over
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 5.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='backgroundImage'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter SourceOverCompositing(Texture2D backgroundImage) {
			return Filter("CISourceOverCompositing", new Dictionary<string, object>() {
				{"inputBackgroundImage",CIImage.FromTexture2D(backgroundImage)}
			});
		}

		/// <summary>Maps color intensity from the sRGB color space to a linear gamma curve.</summary>
		/// <remarks>
		/// <p></p><b>CISRGBToneCurveToLinear</b>
		/// <p class="abstract">Maps color intensity from the sRGB color space to a linear gamma curve.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryNonSquarePixels</code>
		/// ,
		/// <code>CICategoryInterlaced</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryColorAdjustment</code>
		/// <p></p><b>Localized Display Name</b>
		/// sRGB Tone Curve to Linear
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in iOS 7.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter SRGBToneCurveToLinear() {
			return Filter("CISRGBToneCurveToLinear", new Dictionary<string, object>() {
			});
		}

		/// <summary>Generates a starburst pattern that is similar to a supernova; can be used to simulate a lens flare.</summary>
		/// <remarks>
		/// <p></p><b>CIStarShineGenerator</b>
		/// <p class="abstract">Generates a starburst pattern that is similar to a supernova; can be used to simulate a lens flare.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>color</em>
		///     
		///     
		///       <p>
		///         The color of the flare. A
		///         <c>Color3</c>
		///         object whose display name is Color.
		///       </p>
		///     
		///     
		///       <em>radius</em>
		///     
		///     
		///       <p>
		///         Controls the size of the flare. An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeDistance</code>
		///         and whose display name is Radius.
		///       </p>
		///       <p>Default value: 50.00</p>
		///     
		///     
		///       <em>crossScale</em>
		///     
		///     
		///       <p>
		///         Controls the ratio of the cross flare size relative to the round central flare. An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeScalar</code>
		///         and whose display name is Cross Scale.
		///       </p>
		///       <p>Default value: 15.00</p>
		///     
		///     
		///       <em>crossAngle</em>
		///     
		///     
		///       <p>
		///         Controls the angle of the flare. An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeAngle</code>
		///         and whose display name is Cross Angle.
		///       </p>
		///       <p>Default value: 0.60</p>
		///     
		///     
		///       <em>crossOpacity</em>
		///     
		///     
		///       <p>
		///         Controls the thickness of the cross flare. An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeScalar</code>
		///         and whose display name is Cross Opacity.
		///       </p>
		///       <p>Default value: -2.00</p>
		///     
		///     
		///       <em>crossWidth</em>
		///     
		///     
		///       <p>
		///         Has the same overall effect as the
		///         <code>inputCrossOpacity</code>
		///         parameter. An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeDistance</code>
		///         and whose display name is Cross Width.
		///       </p>
		///       <p>Default value: 2.50</p>
		///     
		///     
		///       <em>epsilon</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeScalar</code>
		///         and whose display name is Epsilon.
		///       </p>
		///       <p>Default value: -2.00</p>
		///     
		///   
		/// 
		/// 
		///   <p></p><b>Discussion</b>
		///   <p>The output image is typically used as input to another filter.</p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryGenerator</code>
		/// <p></p><b>Localized Display Name</b>
		/// Star Shine
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='color'>A color in RGBA format</param>
		/// <param name='radius'></param>
		/// <param name='crossScale'></param>
		/// <param name='crossAngle'></param>
		/// <param name='crossOpacity'></param>
		/// <param name='crossWidth'></param>
		/// <param name='epsilon'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter StarShineGenerator(Color32 color, float radius, float crossScale, float crossAngle, float crossOpacity, float crossWidth, float epsilon) {
			return Filter("CIStarShineGenerator", new Dictionary<string, object>() {
				{"inputColor",CIColor.FromColor32(color)},
				{"inputRadius",radius},
				{"inputCrossScale",crossScale},
				{"inputCrossAngle",crossAngle},
				{"inputCrossOpacity",crossOpacity},
				{"inputCrossWidth",crossWidth},
				{"inputEpsilon",epsilon}
			});
		}

		/// <summary>Rotates the source image by the specified angle in radians.</summary>
		/// <remarks>
		/// <p></p><b>CIStraightenFilter</b>
		/// <p class="abstract">Rotates the source image by the specified angle in radians.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>angle</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeScalar</code>
		///         and whose display name is Angle.
		///       </p>
		///       <p>Default value: 0.00</p>
		///     
		///   
		/// 
		/// 
		///   <p></p><b>Discussion</b>
		///   <p>The image is scaled and cropped so that the rotated image fits the extent of the input image.</p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryGeometryAdjustment</code>
		/// <p></p><b>Localized Display Name</b>
		/// Straighten
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.7 and later and in iOS 5.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='angle'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter StraightenFilter(float angle) {
			return Filter("CIStraightenFilter", new Dictionary<string, object>() {
				{"inputAngle",angle}
			});
		}

		/// <summary>Generates a stripe pattern.</summary>
		/// <remarks>
		/// <p></p><b>CIStripesGenerator</b>
		/// <p class="abstract">Generates a stripe pattern.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>color0</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>Color3</c>
		///         object whose display name is Color 1.
		///       </p>
		///     
		///     
		///       <em>color1</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>Color3</c>
		///         object whose display name is Color 2.
		///       </p>
		///     
		///     
		///       <em>width</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeDistance</code>
		///         and whose display name is Width.
		///       </p>
		///       <p>Default value: 80.00</p>
		///     
		///     
		///       <em>sharpness</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeScalar</code>
		///         and whose display name is Sharpness.
		///       </p>
		///       <p>Default value: 1.00</p>
		///     
		///   
		/// 
		/// 
		///   <p></p><b>Discussion</b>
		///   <p>You can control the color of the stripes, the spacing, and the contrast.</p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryGenerator</code>
		/// <p></p><b>Localized Display Name</b>
		/// Stripes
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='color0'>A color in RGBA format</param>
		/// <param name='color1'>A color in RGBA format</param>
		/// <param name='width'></param>
		/// <param name='sharpness'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter StripesGenerator(Color32 color0, Color32 color1, float width, float sharpness) {
			return Filter("CIStripesGenerator", new Dictionary<string, object>() {
				{"inputColor0",CIColor.FromColor32(color0)},
				{"inputColor1",CIColor.FromColor32(color1)},
				{"inputWidth",width},
				{"inputSharpness",sharpness}
			});
		}

		/// <summary>Transitions from one image to another by simulating a swiping action.</summary>
		/// <remarks>
		/// <p></p><b>CISwipeTransition</b>
		/// <p class="abstract">Transitions from one image to another by simulating a swiping action.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>targetImage</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>Texture2D</c>
		///         object whose display name is Target Image.
		///       </p>
		///     
		///     
		///       <em>extent</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeRectangle</code>
		///         and whose display name is Extent.
		///       </p>
		///       <p>Default value: [0 0 300 300]</p>
		///     
		///     
		///       <em>color</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>Color3</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeOpaqueColor</code>
		///         and whose display name is Color.
		///       </p>
		///     
		///     
		///       <em>time</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeTime</code>
		///         and whose display name is Time.
		///       </p>
		///       <p>Default value: 0.00</p>
		///     
		///     
		///       <em>angle</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeAngle</code>
		///         and whose display name is Angle.
		///       </p>
		///       <p>Default value: 0.00</p>
		///     
		///     
		///       <em>width</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeDistance</code>
		///         and whose display name is Width.
		///       </p>
		///       <p>Default value: 300.00</p>
		///     
		///     
		///       <em>opacity</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeScalar</code>
		///         and whose display name is Opacity.
		///       </p>
		///       <p>Default value: 0.00</p>
		///     
		///   
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryTransition</code>
		/// <p></p><b>Localized Display Name</b>
		/// Swipe
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='targetImage'></param>
		/// <param name='extent'>An array of floats representing a vector</param>
		/// <param name='color'>A color in RGBA format</param>
		/// <param name='time'></param>
		/// <param name='angle'></param>
		/// <param name='width'></param>
		/// <param name='opacity'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter SwipeTransition(Texture2D targetImage, float[] extent, Color32 color, float time, float angle, float width, float opacity) {
			return Filter("CISwipeTransition", new Dictionary<string, object>() {
				{"inputTargetImage",CIImage.FromTexture2D(targetImage)},
				{"inputExtent",new CIVector("[" + string.Join(" ", Array.ConvertAll(extent, x => x.ToString())) + "]")},
				{"inputColor",CIColor.FromColor32(color)},
				{"inputTime",time},
				{"inputAngle",angle},
				{"inputWidth",width},
				{"inputOpacity",opacity}
			});
		}

		/// <summary>Adapts the reference white point for an image.</summary>
		/// <remarks>
		/// <p></p><b>CITemperatureAndTint</b>
		/// <p class="abstract">Adapts the reference white point for an image.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>neutral</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeOffset</code>
		///         and whose display name is Neutral.
		///       </p>
		///       <p>Default value: [6500, 0]</p>
		///     
		///     
		///       <em>targetNeutral</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeOffset</code>
		///         and whose display name is TargetNeutral
		///       </p>
		///       <p>Default value: [6500, 0]</p>
		///     
		///   
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryNonSquarePixels</code>
		/// ,
		/// <code>CICategoryInterlaced</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryColorAdjustment</code>
		/// <p></p><b>Localized Display Name</b>
		/// Temperature and Tint
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.7 and later and in iOS 5.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='neutral'>An array of floats representing a vector</param>
		/// <param name='targetNeutral'>An array of floats representing a vector</param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter TemperatureAndTint(float[] neutral, float[] targetNeutral) {
			return Filter("CITemperatureAndTint", new Dictionary<string, object>() {
				{"inputNeutral",new CIVector("[" + string.Join(" ", Array.ConvertAll(neutral, x => x.ToString())) + "]")},
				{"inputTargetNeutral",new CIVector("[" + string.Join(" ", Array.ConvertAll(targetNeutral, x => x.ToString())) + "]")}
			});
		}

		/// <summary>Adjusts tone response of the R, G, and B channels of an image.</summary>
		/// <remarks>
		/// <p></p><b>CIToneCurve</b>
		/// <p class="abstract">Adjusts tone response of the R, G, and B channels of an image.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>point0</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeOffset</code>
		///         .
		///       </p>
		///       <p>Default value: [0, 0]</p>
		///     
		///     
		///       <em>point1</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeOffset</code>
		///         .
		///       </p>
		///       <p>Default value: [0.25, 0.25]</p>
		///     
		///     
		///       <em>point2</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeOffset</code>
		///         .
		///       </p>
		///       <p>Default value: [0.5, 0.5]</p>
		///     
		///     
		///       <em>point3</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeOffset</code>
		///         .
		///       </p>
		///       <p>Default value: [0.75, 0.75]</p>
		///     
		///     
		///       <em>point4</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeOffset</code>
		///         .
		///       </p>
		///       <p>Default value: [1, 1]</p>
		///     
		///   
		/// 
		/// 
		///   <p></p><b>Discussion</b>
		///   <p>The input points are five x,y values that are interpolated using a spline curve. The curve is applied in a perceptual (gamma 2) version of the working space.</p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryNonSquarePixels</code>
		/// ,
		/// <code>CICategoryInterlaced</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryColorAdjustment</code>
		/// <p></p><b>Localized Display Name</b>
		/// Tone Curve
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.7 and later and in iOS 5.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='point0'>An array of floats representing a vector</param>
		/// <param name='point1'>An array of floats representing a vector</param>
		/// <param name='point2'>An array of floats representing a vector</param>
		/// <param name='point3'>An array of floats representing a vector</param>
		/// <param name='point4'>An array of floats representing a vector</param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter ToneCurve(float[] point0, float[] point1, float[] point2, float[] point3, float[] point4) {
			return Filter("CIToneCurve", new Dictionary<string, object>() {
				{"inputPoint0",new CIVector("[" + string.Join(" ", Array.ConvertAll(point0, x => x.ToString())) + "]")},
				{"inputPoint1",new CIVector("[" + string.Join(" ", Array.ConvertAll(point1, x => x.ToString())) + "]")},
				{"inputPoint2",new CIVector("[" + string.Join(" ", Array.ConvertAll(point2, x => x.ToString())) + "]")},
				{"inputPoint3",new CIVector("[" + string.Join(" ", Array.ConvertAll(point3, x => x.ToString())) + "]")},
				{"inputPoint4",new CIVector("[" + string.Join(" ", Array.ConvertAll(point4, x => x.ToString())) + "]")}
			});
		}

		/// <summary>Maps a triangular portion of an input image to create a kaleidoscope effect.</summary>
		/// <remarks>
		/// <p></p><b>CITriangleKaleidoscope</b>
		/// <p class="abstract">Maps a triangular portion of an input image to create a kaleidoscope effect.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>point</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypePosition</code>
		///         .
		///       </p>
		///       <p>Default value: [150 150]</p>
		///     
		///     
		///       <em>size</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeScalar</code>
		///         .
		///       </p>
		///       <p>Default value: 700.00</p>
		///     
		///     
		///       <em>rotation</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeAngle</code>
		///         .
		///       </p>
		///       <p>Default value: –0.36</p>
		///     
		///     
		///       <em>decay</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeScalar</code>
		///         .
		///       </p>
		///       <p>Default: 0.85</p>
		///     
		///   
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryTileEffect</code>
		/// <p></p><b>Localized Display Name</b>
		/// Triangle Kaleidoscope
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='point'>An array of floats representing a vector</param>
		/// <param name='size'></param>
		/// <param name='rotation'></param>
		/// <param name='decay'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter TriangleKaleidoscope(float[] point, float size, float rotation, float decay) {
			return Filter("CITriangleKaleidoscope", new Dictionary<string, object>() {
				{"inputPoint",new CIVector("[" + string.Join(" ", Array.ConvertAll(point, x => x.ToString())) + "]")},
				{"inputSize",size},
				{"inputRotation",rotation},
				{"inputDecay",decay}
			});
		}

		/// <summary>Produces a tiled image from a source image by rotating the source image at increments of 30 degrees.</summary>
		/// <remarks>
		/// <p></p><b>CITwelvefoldReflectedTile</b>
		/// <p class="abstract">Produces a tiled image from a source image by rotating the source image at increments of 30 degrees.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>center</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypePosition</code>
		///         and whose display name is Center.
		///       </p>
		///       <p>Default value: [150 150]</p>
		///     
		///     
		///       <em>angle</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeAngle</code>
		///         and whose display name is Angle.
		///       </p>
		///       <p>Default value: 0.00</p>
		///     
		///     
		///       <em>width</em>
		///     
		///     
		///       <p>
		///         The width, along with the
		///         <code>inputCenter</code>
		///         parameter, defines the portion of the image to tile. An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeDistance</code>
		///         and whose display name is Width.
		///       </p>
		///       <p>Default value: 100.00</p>
		///     
		///   
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryTileEffect</code>
		/// <p></p><b>Localized Display Name</b>
		/// CITwelvefoldReflectedTile
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.5 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='center'>An array of floats representing a vector</param>
		/// <param name='angle'></param>
		/// <param name='width'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter TwelvefoldReflectedTile(float[] center, float angle, float width) {
			return Filter("CITwelvefoldReflectedTile", new Dictionary<string, object>() {
				{"inputCenter",new CIVector("[" + string.Join(" ", Array.ConvertAll(center, x => x.ToString())) + "]")},
				{"inputAngle",angle},
				{"inputWidth",width}
			});
		}

		/// <summary>Rotates pixels around a point to give a twirling effect.</summary>
		/// <remarks>
		/// <p></p><b>CITwirlDistortion</b>
		/// <p class="abstract">Rotates pixels around a point to give a twirling effect.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>center</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypePosition</code>
		///         and whose display name is Center.
		///       </p>
		///       <p>Default value: [150 150]</p>
		///     
		///     
		///       <em>radius</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeDistance</code>
		///         and whose display name is Radius.
		///       </p>
		///       <p>Default value: 300.00</p>
		///     
		///     
		///       <em>angle</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeAngle</code>
		///         and whose display name is Angle.
		///       </p>
		///       <p>Default value: 3.14</p>
		///     
		///   
		/// 
		/// 
		///   <p></p><b>Discussion</b>
		///   <p>You can specify the number of rotations as well as the center and radius of the effect.</p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryDistortionEffect</code>
		/// <p></p><b>Localized Display Name</b>
		/// Twirl Distortion
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='center'>An array of floats representing a vector</param>
		/// <param name='radius'></param>
		/// <param name='angle'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter TwirlDistortion(float[] center, float radius, float angle) {
			return Filter("CITwirlDistortion", new Dictionary<string, object>() {
				{"inputCenter",new CIVector("[" + string.Join(" ", Array.ConvertAll(center, x => x.ToString())) + "]")},
				{"inputRadius",radius},
				{"inputAngle",angle}
			});
		}

		/// <summary>Increases the contrast of the edges between pixels of different colors in an image.</summary>
		/// <remarks>
		/// <p></p><b>CIUnsharpMask</b>
		/// <p class="abstract">Increases the contrast of the edges between pixels of different colors in an image.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>radius</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeDistance</code>
		///         and whose display name is Radius.
		///       </p>
		///       <p>Default value: 2.50</p>
		///     
		///     
		///       <em>intensity</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeScalar</code>
		///         and whose display name is Intensity.
		///       </p>
		///       <p>Default value: 0.50</p>
		///     
		///   
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategorySharpen</code>
		/// <p></p><b>Localized Display Name</b>
		/// Unsharp Mask
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='radius'></param>
		/// <param name='intensity'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter UnsharpMask(float radius, float intensity) {
			return Filter("CIUnsharpMask", new Dictionary<string, object>() {
				{"inputRadius",radius},
				{"inputIntensity",intensity}
			});
		}

		/// <summary>Adjusts the saturation of an image while keeping pleasing skin tones.</summary>
		/// <remarks>
		/// <p></p><b>CIVibrance</b>
		/// <p class="abstract">Adjusts the saturation of an image while keeping pleasing skin tones.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>amount</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeScalar</code>
		///         and whose display name is Amount.
		///       </p>
		///     
		///   
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryNonSquarePixels</code>
		/// ,
		/// <code>CICategoryInterlaced</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryColorAdjustment</code>
		/// <p></p><b>Localized Display Name</b>
		/// Vibrance
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.7 and later and in iOS 5.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='amount'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter Vibrance(float amount) {
			return Filter("CIVibrance", new Dictionary<string, object>() {
				{"inputAmount",amount}
			});
		}

		/// <summary>Reduces the brightness of an image at the periphery.</summary>
		/// <remarks>
		/// <p></p><b>CIVignette</b>
		/// <p class="abstract">Reduces the brightness of an image at the periphery.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>radius</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeDistance</code>
		///         and whose display name is Radius.
		///       </p>
		///       <p>Default value: 1.00</p>
		///     
		///     
		///       <em>intensity</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeScalar</code>
		///         and whose display name is Intensity.
		///       </p>
		///       <p>Default value: 0.0</p>
		///     
		///   
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryNonSquarePixels</code>
		/// ,
		/// <code>CICategoryInterlaced</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryColorEffect</code>
		/// <p></p><b>Localized Display Name</b>
		/// Vignette
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.9 and later and in iOS 5.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='radius'></param>
		/// <param name='intensity'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter Vignette(float radius, float intensity) {
			return Filter("CIVignette", new Dictionary<string, object>() {
				{"inputRadius",radius},
				{"inputIntensity",intensity}
			});
		}

		/// <summary>Modifies the brightness of an image around the periphery of a specified region.</summary>
		/// <remarks>
		/// <p></p><b>CIVignetteEffect</b>
		/// <p class="abstract">Modifies the brightness of an image around the periphery of a specified region.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>center</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypePosition</code>
		///         and whose display name is Center.
		///       </p>
		///       <p>Default value: [150 150] Identity: (null)</p>
		///     
		///     
		///       <em>intensity</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeScalar</code>
		///         and whose display name is Intensity.
		///       </p>
		///       <p>Default value: 1.00 Minimum: 0.00 Maximum: 0.00 Slider minimum: 0.00 Slider maximum: 1.00 Identity: 0.00</p>
		///     
		///     
		///       <em>radius</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose display name is Radius.
		///       </p>
		///       <p>Default value: 0.00 Minimum: 0.00 Maximum: 0.00 Slider minimum: 0.00 Slider maximum: 0.00 Identity: 0.00</p>
		///     
		///   
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryNonSquarePixels</code>
		/// ,
		/// <code>CICategoryInterlaced</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryColorEffect</code>
		/// <p></p><b>Localized Display Name</b>
		/// Vignette Effect
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.9 and later and in iOS 7.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='center'>An array of floats representing a vector</param>
		/// <param name='intensity'></param>
		/// <param name='radius'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter VignetteEffect(float[] center, float intensity, float radius) {
			return Filter("CIVignetteEffect", new Dictionary<string, object>() {
				{"inputCenter",new CIVector("[" + string.Join(" ", Array.ConvertAll(center, x => x.ToString())) + "]")},
				{"inputIntensity",intensity},
				{"inputRadius",radius}
			});
		}

		/// <summary>Rotates pixels around a point to simulate a vortex.</summary>
		/// <remarks>
		/// <p></p><b>CIVortexDistortion</b>
		/// <p class="abstract">Rotates pixels around a point to simulate a vortex.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>center</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>float[]</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypePosition</code>
		///         and whose display name is Center.
		///       </p>
		///       <p>Default value: [150 150]</p>
		///     
		///     
		///       <em>radius</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeDistance</code>
		///         and whose display name is Radius.
		///       </p>
		///       <p>Default value: 300.00</p>
		///     
		///     
		///       <em>angle</em>
		///     
		///     
		///       <p>
		///         An
		///         <c>float</c>
		///         object whose attribute type is
		///         <code>CIAttributeTypeAngle</code>
		///         and whose display name is Angle.
		///       </p>
		///       <p>Default value: 56.55</p>
		///     
		///   
		/// 
		/// 
		///   <p></p><b>Discussion</b>
		///   <p>You can specify the number of rotations as well the center and radius of the effect.</p>
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryDistortionEffect</code>
		/// <p></p><b>Localized Display Name</b>
		/// Vortex Distortion
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 6.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='center'>An array of floats representing a vector</param>
		/// <param name='radius'></param>
		/// <param name='angle'></param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter VortexDistortion(float[] center, float radius, float angle) {
			return Filter("CIVortexDistortion", new Dictionary<string, object>() {
				{"inputCenter",new CIVector("[" + string.Join(" ", Array.ConvertAll(center, x => x.ToString())) + "]")},
				{"inputRadius",radius},
				{"inputAngle",angle}
			});
		}

		/// <summary>Adjusts the reference white point for an image and maps all colors in the source using the new reference.</summary>
		/// <remarks>
		/// <p></p><b>CIWhitePointAdjust</b>
		/// <p class="abstract">Adjusts the reference white point for an image and maps all colors in the source using the new reference.</p>
		/// 
		///   <p></p><b>Parameters</b>
		///   <p></p>
		///     
		///       <em>color</em>
		///     
		///     
		///       <p>
		///         A
		///         <c>Color3</c>
		///         object whose display name is Color.
		///       </p>
		///     
		///   
		/// 
		/// <p></p><b>Member of</b>
		/// <code>CICategoryBuiltIn</code>
		/// ,
		/// <code>CICategoryNonSquarePixels</code>
		/// ,
		/// <code>CICategoryInterlaced</code>
		/// ,
		/// <code>CICategoryStillImage</code>
		/// ,
		/// <code>CICategoryVideo</code>
		/// ,
		/// <code>CICategoryColorAdjustment</code>
		/// <p></p><b>Localized Display Name</b>
		/// White Point Adjust
		/// 
		/// 
		///   <p></p><b>Availability</b>
		///   <ul>
		///     <li>Available in OS X v10.4 and later and in iOS 5.0 and later.</li>
		///   </ul>
		/// 
		/// </remarks>
		/// <param name='color'>A color in RGBA format</param>
		/// <returns>This object itself, for chaining filters</returns>
		public ImageFilter WhitePointAdjust(Color32 color) {
			return Filter("CIWhitePointAdjust", new Dictionary<string, object>() {
				{"inputColor",CIColor.FromColor32(color)}
			});
		}


	}
}