/*
 *  OpenSlide, a library for reading whole slide image files
 *
 *  Copyright (c) 2007-2014 Carnegie Mellon University
 *  All rights reserved.
 *
 *  OpenSlide is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU Lesser General Public License as
 *  published by the Free Software Foundation, version 2.1.
 *
 *  OpenSlide is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU Lesser General Public License for more details.
 *
 *  You should have received a copy of the GNU Lesser General Public
 *  License along with OpenSlide. If not, see
 *  <http://www.gnu.org/licenses/>.
 *
 */

/**
 * @file openslide.h
 * The API for the OpenSlide library.
 *
 * All functions except openslide_close() are thread-safe.
 * See the openslide_close() documentation for its restrictions.
 */

using System;
using System.Runtime.InteropServices;

namespace OpenSlideServer
{
    public class OpenSlideInterface
    {
        internal const string OPENSLIDE_PROPERTY_NAME_MPP_X = "openslide.mpp-x";
        internal const string OPENSLIDE_PROPERTY_NAME_MPP_Y = "openslide.mpp-y";

        /**
         * Quickly determine whether a whole slide image is recognized.
         *
         * If OpenSlide recognizes the file referenced by @p filename, return a
         * string identifying the slide format vendor.  This is equivalent to the
         * value of the #OPENSLIDE_PROPERTY_NAME_VENDOR property.  Calling
         * openslide_open() on this file will return a valid OpenSlide object or
         * an OpenSlide object in error state.
         *
         * Otherwise, return NULL.  Calling openslide_open() on this file will also
         * return NULL.
         *
         * @param filename The filename to check.
         * @return An identification of the format vendor for this file, or NULL.
         * @since 3.4.0
         */
        // const char* openslide_detect_vendor(const char* filename);
        [DllImport("libopenslide-0.dll", EntryPoint = "openslide_detect_vendor")]
        public static extern IntPtr Openslide_detect_vendor([MarshalAs(UnmanagedType.LPStr)]string lpString);

        /**
         * Get the value of a single property.
         * Certain vendor-specific metadata properties may exist within a whole slide image.
         * They are encoded as key-value pairs.
         * This call provides the value of the property given by name.
         */
        // const char* openslide_get_property_value(openslide_t *osr,const char* name);
        [DllImport("libopenslide-0.dll", EntryPoint = "openslide_get_property_value")]
        public static extern IntPtr Openslide_get_property_value(IntPtr osr, [MarshalAs(UnmanagedType.LPStr)] string lpString);

        /**
         * Open a whole slide image.
         *
         * This function can be expensive; avoid calling it unnecessarily.  For
         * example, a tile server should not call openslide_open() on every tile
         * request.  Instead, it should maintain a cache of OpenSlide objects and
         * reuse them when possible.
         *
         * @param filename The filename to open.
         * @return
         *         On success, a new OpenSlide object.
         *         If the file is not recognized by OpenSlide, NULL.
         *         If the file is recognized but an error occurred, an OpenSlide
         *         object in error state.
         */
        // openslide_t* openslide_open(const char* filename);
        [DllImport("libopenslide-0.dll", EntryPoint = "openslide_open")]
        public static extern IntPtr Openslide_open([MarshalAs(UnmanagedType.LPStr)]string lpString);

        /**
         * Get the number of levels in the whole slide image.
         *
         * @param osr The OpenSlide object.
         * @return The number of levels, or -1 if an error occurred.
         * @since 3.3.0
         */
        // int32_t openslide_get_level_count(openslide_t *osr);
        [DllImport("libopenslide-0.dll", EntryPoint = "openslide_get_level_count")]
        public static extern Int32 Openslide_get_level_count(IntPtr osr);

        /**
         * Get the dimensions of level 0 (the largest level). Exactly
         * equivalent to calling openslide_get_level_dimensions(osr, 0, w, h).
         *
         * @param osr The OpenSlide object.
         * @param[out] w The width of the image, or -1 if an error occurred.
         * @param[out] h The height of the image, or -1 if an error occurred.
         * @since 3.3.0
         */
        // void openslide_get_level0_dimensions(openslide_t* osr, int64_t* w, int64_t* h);
        [DllImport("libopenslide-0.dll", EntryPoint = "openslide_get_level0_dimensions")]
        public static extern unsafe void Openslide_get_level0_dimensions(IntPtr osr, Int64* w, Int64* h);

        /**
         * Get the dimensions of a level.
         *
         * @param osr The OpenSlide object.
         * @param level The desired level.
         * @param[out] w The width of the image, or -1 if an error occurred
         *               or the level was out of range.
         * @param[out] h The height of the image, or -1 if an error occurred
         *               or the level was out of range.
         * @since 3.3.0
         */
        // void openslide_get_level_dimensions(openslide_t* osr, 
        //     int32_t level, 
        //     int64_t* w, 
        //     int64_t* h);
        [DllImport("libopenslide-0.dll", EntryPoint = "openslide_get_level_dimensions")]
        public static extern unsafe void Openslide_get_level_dimensions(IntPtr osr, 
            Int32 level, 
            Int64* w, 
            Int64* h);

        /**
         * Get the downsampling factor of a given level.
         *
         * @param osr The OpenSlide object.
         * @param level The desired level.
         * @return The downsampling factor for this level, or -1.0 if an error occurred
         *         or the level was out of range.
         * @since 3.3.0
         */
        // double openslide_get_level_downsample(openslide_t* osr, int32_t level);
        [DllImport("libopenslide-0.dll", EntryPoint = "openslide_get_level_downsample")]
        public static extern Double Openslide_get_level_downsample(IntPtr osr, Int32 level);

        /**
         * Get the best level to use for displaying the given downsample.
         *
         * @param osr The OpenSlide object.
         * @param downsample The downsample factor.
         * @return The level identifier, or -1 if an error occurred.
         * @since 3.3.0
         */
        // int32_t openslide_get_best_level_for_downsample(openslide_t* osr,
        //                double downsample);
        [DllImport("libopenslide-0.dll", EntryPoint = "openslide_get_best_level_for_downsample")]
        public static extern Int32 Openslide_get_best_level_for_downsample(IntPtr osr, 
            Double downsample);

        /**
         * Copy pre-multiplied ARGB data from a whole slide image.
         *
         * This function reads and decompresses a region of a whole slide
         * image into the specified memory location. @p dest must be a valid
         * pointer to enough memory to hold the region, at least (@p w * @p h * 4)
         * bytes in length. If an error occurs or has occurred, then the memory
         * pointed to by @p dest will be cleared.
         *
         * @param osr The OpenSlide object.
         * @param dest The destination buffer for the ARGB data.
         * @param x The top left x-coordinate, in the level 0 reference frame.
         * @param y The top left y-coordinate, in the level 0 reference frame.
         * @param level The desired level.
         * @param w The width of the region. Must be non-negative.
         * @param h The height of the region. Must be non-negative.
         */
        // void openslide_read_region(openslide_t* osr,
        //       uint32_t* dest,
        //       int64_t x, int64_t y,
        //       int32_t level,
        //       int64_t w, int64_t h);
        [DllImport("libopenslide-0.dll", EntryPoint = "openslide_read_region")]
        public static extern unsafe void Openslide_read_region(IntPtr osr, 
            IntPtr dest, 
            Int64 x, Int64 y, 
            Int32 level, 
            Int64 w, Int64 h);

        /**
         * Close an OpenSlide object.
         * No other threads may be using the object.
         * After this call returns, the object cannot be used anymore.
         *
         * @param osr The OpenSlide object.
         */
        // void openslide_close(openslide_t* osr);
        [DllImport("libopenslide-0.dll", EntryPoint = "openslide_close")]
        public static extern void Openslide_close(IntPtr osr);

        /**
         * Get the current error string.
         *
         * For a given OpenSlide object, once this function returns a non-NULL
         * value, the only useful operation on the object is to call
         * openslide_close() to free its resources.
         *
         * @param osr The OpenSlide object.
         * @return A string describing the original error that caused
         * the problem, or NULL if no error has occurred.
         * @since 3.2.0
         *
         */
        // const char* openslide_get_error(openslide_t * osr);
        [DllImport("libopenslide-0.dll", EntryPoint = "openslide_get_error")]
        public static extern string Openslide_get_error(IntPtr osr);   
    }
}
