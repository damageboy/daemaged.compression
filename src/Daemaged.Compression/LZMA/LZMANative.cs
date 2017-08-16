using System;
using System.Runtime.InteropServices;
using System.Security;
using Daemaged.Compression.Util;

namespace Daemaged.Compression.LZMA
{
  [SuppressUnmanagedCodeSecurity] 
  internal class LZMANative
  {
    private static object _staticSyncRoot;
    private static IntPtr _nativeModulePtr;
    public const uint LZMA_PB_MIN = 0;
    public const uint LZMA_PB_MAX = 4;
    public const uint LZMA_PB_DEFAULT = 2;
    public const uint LZMA_DICT_SIZE_MIN = 4096;
    public const uint LZMA_DICT_SIZE_DEFAULT = 1 << 23;
    public const uint LZMA_LCLP_MIN = 0;
    public const uint LZMA_LCLP_MAX = 4;
    public const uint LZMA_LC_DEFAULT = 3;
    public const uint LZMA_LP_DEFAULT = 0;
    public const uint LZMA_PRESET_EXTREME = 1U << 31;

    static LZMANative()
    {
      Initialize();
    }

    private static void Initialize()
    {
      // If we're on Linux / MacOsX, just let the platform find the .so / .dylib, 
      // non of our bussiness, for now
      if (Environment.OSVersion.Platform != PlatformID.Win32NT)
        return;

      lock (_staticSyncRoot) {        
        _nativeModulePtr = NativePreloadHelper.Preload(LIBLZMA);
      }
    }


    internal const string LIBLZMA = "liblzma";

    internal const ulong LZMA_VLI_UNKNOWN = ulong.MaxValue;
    //LZMA1 Filter ID
    internal const ulong LZMA_FILTER_LZMA1 = 0x4000000000000001UL;
    //LZMA2 Filter ID
    internal const ulong LZMA_FILTER_LZMA2 = 0x21;
    //LZMA Delta Filter ID
    internal const ulong LZMA_FILTER_DELTA = 0x03; 


    [DllImport(LIBLZMA, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe LZMAStatus lzma_auto_decoder(LZMAStreamNative* strm, ulong memlimit, uint flags);

    // \brief       Encode or decode data
    //
    // Once the lzma_stream has been successfully initialized (e.g. with
    // lzma_stream_encoder()), the actual encoding or decoding is done
    // using this function. The application has to update strm->next_in,
    // strm->avail_in, strm->next_out, and strm->avail_out to pass input
    // to and get output from liblzma.
    //
    // See the description of the coder-specific initialization function to find
    // out what `action' values are supported by the coder. See documentation of
    // lzma_ret for the possible return values.
    //
    [DllImport(LIBLZMA, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe LZMAStatus lzma_code(LZMAStreamNative* strm, LZMAAction action);

    // Free memory allocated for the coder data structures
    //
    // \param       strm    Pointer to lzma_stream that is at least initialized
    //                      with LZMA_STREAM_INIT.
    //
    // After lzma_end(strm), strm->internal is guaranteed to be NULL. No other
    // members of the lzma_stream structure are touched.
    //
    // \note        zlib indicates an error if application end()s unfinished
    //              stream structure. liblzma doesn't do this, and assumes that
    //              application knows what it is doing.
    [DllImport(LIBLZMA, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe void lzma_end(void* strm);

    [DllImport(LIBLZMA, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe LZMAStatus lzma_alone_encoder(LZMAStreamNative* strm, ref LZMAOptionLZMA options);

    [DllImport(LIBLZMA, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe LZMAStatus lzma_easy_encoder(LZMAStreamNative* strm, uint preset, LZMACheck check);

    // Initialize .xz Stream encoder using a custom filter chain
    //
    // \param       strm    Pointer to properly prepared lzma_stream
    // \param       filters Array of filters. This must be terminated with
    //                      filters[n].id = LZMA_VLI_UNKNOWN. See filter.h for
    //                      more information.
    // \param       check   Type of the integrity check to calculate from
    //                      uncompressed data.
    //
    // \return      - LZMA_OK: Initialization was successful.
    //              - LZMA_MEM_ERROR
    //              - LZMA_OPTIONS_ERROR
    //              - LZMA_PROG_ERROR
    [DllImport(LIBLZMA, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe LZMAStatus lzma_stream_encoder(LZMAStreamNative* strm, void* filters,
                                                               LZMACheck check);


    // Get the memory usage of decoder filter chain
    //
    // This function is currently supported only when *strm has been initialized
    // with a function that takes a memlimit argument. With other functions, you
    // should use e.g. lzma_raw_encoder_memusage() or lzma_raw_decoder_memusage()
    // to estimate the memory requirements.
    //
    // This function is useful e.g. after LZMA_MEMLIMIT_ERROR to find out how big
    // the memory usage limit should have been to decode the input. Note that
    // this may give misleading information if decoding .xz Streams that have
    // multiple Blocks, because each Block can have different memory requirements.
    //
    // \return      Rough estimate of how much memory is currently allocated
    //              for the filter decoders. If no filter chain is currently
    //              allocated, some non-zero value is still returned, which is
    //              less than or equal to what any filter chain would indicate
    //              as its memory requirement.
    //
    //              If this function isn't supported by *strm or some other error
    //              occurs, zero is returned.
    [DllImport(LIBLZMA, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe ulong lzma_memusage(LZMAStreamNative* strm);

    // Get the current memory usage limit
    //
    // This function is supported only when *strm has been initialized with
    // a function that takes a memlimit argument.
    //
    // \return      On success, the current memory usage limit is returned
    //              (always non-zero). On error, zero is returned.
    //
    [DllImport(LIBLZMA, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe ulong lzma_memlimit_get(LZMAStreamNative* strm);

    // Set the memory usage limit
    // 
    // This function is supported only when *strm has been initialized with
    // a function that takes a memlimit argument.
    // 
    // \return      - LZMA_OK: New memory usage limit successfully set.
    //              - LZMA_MEMLIMIT_ERROR: The new limit is too small.
    //                The limit was not changed.
    //              - LZMA_PROG_ERROR: Invalid arguments, e.g. *strm doesn't
    //                support memory usage limit or memlimit was zero.
    [DllImport(LIBLZMA, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe LZMAStatus lzma_memlimit_set(LZMAStreamNative* strm, ulong memlimit);


    // Set a compression preset to lzma_options_lzma structure
    //
    // 0 is the fastest and 9 is the slowest. These match the switches -0 .. -9
    // of the xz command line tool. In addition, it is possible to bitwise-or
    // flags to the preset. Currently only LZMA_PRESET_EXTREME is supported.
    // The flags are defined in container.h, because the flags are used also
    // with lzma_easy_encoder().
    //
    // The preset values are subject to changes between liblzma versions.
    //
    // This function is available only if LZMA1 or LZMA2 encoder has been enabled
    // when building liblzma.
    [DllImport(LIBLZMA, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe bool lzma_lzma_preset(void* options, uint preset);

    ///
    //\brief       Calculate output buffer size for single-call Stream encoder
    //
    //When trying to compress uncompressible data, the encoded size will be
    //slightly bigger than the input data. This function calculates how much
    //output buffer space is required to be sure that lzma_stream_buffer_encode()
    //doesn't return LZMA_BUF_ERROR.
    //
    //The calculated value is not exact, but it is guaranteed to be big enough.
    //The actual maximum output space required may be slightly smaller (up to
    //about 100 bytes). This should not be a problem in practice.
    //
    //If the calculated maximum size doesn't fit into size_t or would make the
    //Stream grow past LZMA_VLI_MAX (which should never happen in practice),
    //zero is returned to indicate the error.
    //
    //\note        The limit calculated by this function applies only to
    //             single-call encoding. Multi-call encoding may (and probably
    //             will) have larger maximum expansion when encoding
    //             uncompressible data. Currently there is no function to
    //              calculate the maximum expansion of multi-call encoding.
    ////
    [DllImport(LIBLZMA, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe IntPtr lzma_stream_buffer_bound(IntPtr uncompressed_size);		

    //
    //\brief       Single-call .xz Stream decoder
    //
    //\param       memlimit    Pointer to how much memory the decoder is allowed
    //                         to allocate. The value pointed by this pointer is
    //                         modified if and only if LZMA_MEMLIMIT_ERROR is
    //                         returned.
    //\param       flags       Bitwise-or of zero or more of the decoder flags:
    //                         LZMA_TELL_NO_CHECK, LZMA_TELL_UNSUPPORTED_CHECK,
    //                         LZMA_CONCATENATED. Note that LZMA_TELL_ANY_CHECK
    //                         is not allowed and will return LZMA_PROG_ERROR.
    //\param       allocator   lzma_allocator for custom allocator functions.
    //                         Set to NULL to use malloc() and free().
    //\param       in          Beginning of the input buffer
    //\param       in_pos      The next byte will be read from in[*in_pos].
    //                         *in_pos is updated only if decoding succeeds.
    //\param       in_size     Size of the input buffer; the first byte that
    //                         won't be read is in[in_size].
    //\param       out         Beginning of the output buffer
    //\param       out_pos     The next byte will be written to out[*out_pos].
    //                         *out_pos is updated only if encoding succeeds.
    //\param       out_size    Size of the out buffer; the first byte into
    //                         which no data is written to is out[out_size].
    //
    //\return      - LZMA_OK: Decoding was successful.
    //             - LZMA_FORMAT_ERROR
    //             - LZMA_OPTIONS_ERROR
    //             - LZMA_DATA_ERROR
    //             - LZMA_NO_CHECK: This can be returned only if using
    //               the LZMA_TELL_NO_CHECK flag.
    //             - LZMA_UNSUPPORTED_CHECK: This can be returned only if using
    //               the LZMA_TELL_UNSUPPORTED_CHECK flag.
    //             - LZMA_MEM_ERROR
    //             - LZMA_MEMLIMIT_ERROR: Memory usage limit was reached.
    //               The minimum required memlimit value was stored to *memlimit.
    //             - LZMA_BUF_ERROR: Output buffer was too small.
    //             - LZMA_PROG_ERROR
    ////

    [DllImport(LIBLZMA, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe LZMAStatus lzma_stream_buffer_decode(ulong* memlimit, uint flags, void* allocator, 
                                                                     void* in_buff, IntPtr* in_pos, IntPtr in_size, 
                                                                     void* out_buff, IntPtr* out_pos, IntPtr out_size);

    ///
    //\brief       Single-call .xz Stream encoder
    //
    //\param       filters     Array of filters. This must be terminated with
    //                         filters[n].id = LZMA_VLI_UNKNOWN. See filter.h
    //                         for more information.
    //\param       check       Type of the integrity check to calculate from
    //                         uncompressed data.
    //\param       allocator   lzma_allocator for custom allocator functions.
    //                         Set to NULL to use malloc() and free().
    //\param       in          Beginning of the input buffer
    //\param       in_size     Size of the input buffer
    //\param       out         Beginning of the output buffer
    //\param       out_pos     The next byte will be written to out[*out_pos].
    //                         *out_pos is updated only if encoding succeeds.
    //\param       out_size    Size of the out buffer; the first byte into
    //                         which no data is written to is out[out_size].
    //
    //\return      - LZMA_OK: Encoding was successful.
    //             - LZMA_BUF_ERROR: Not enough output buffer space.
    //             - LZMA_OPTIONS_ERROR
    //             - LZMA_MEM_ERROR
    //             - LZMA_DATA_ERROR
    //             - LZMA_PROG_ERROR
    ///
    [DllImport(LIBLZMA, CallingConvention = CallingConvention.Cdecl)]
    public static extern unsafe LZMAStatus lzma_stream_buffer_encode(LZMAFilter* filters, LZMACheck check, void* allocator,
                                                                     byte* in_buff, IntPtr in_size,
                                                                     byte* out_buff, IntPtr* out_pos, IntPtr out_size);
  }

#if AMD64
  [StructLayout(LayoutKind.Sequential, Pack = 8)]
#endif
#if I386
  [StructLayout(LayoutKind.Sequential, Pack = 4)]
#endif
  public unsafe struct LZMAFilter
  {
    /**
     * Filter ID
     *
     * Use constants whose name begin with `LZMA_FILTER_' to specify
     * different filters. In an array of lzma_option_filter structures,
     * use LZMA_VLI_UNKNOWN to indicate end of filters.
     */
    public ulong id;

    /**
     * \brief       Pointer to filter-specific options structure
     *
     * If the filter doesn't need options, set this to NULL. If id is
     * set to LZMA_VLI_UNKNOWN, options is ignored, and thus
     * doesn't need be initialized.
     *
     * Some filters support changing the options in the middle of
     * the encoding process. These filters store the pointer of the
     * options structure and communicate with the application via
     * modifications of the options structure.
     */
    public void *options;
  }



#if AMD64
  [StructLayout(LayoutKind.Sequential, Pack = 8)]
#endif
#if I386
  [StructLayout(LayoutKind.Sequential, Pack = 4)]
#endif
  public unsafe struct LZMAStreamNative
  {
    internal void* next_in; /** Pointer to the next input byte. */
    internal IntPtr avail_in; /** Number of available input bytes in next_in. */
    internal ulong total_in; /** Total number of bytes read by liblzma. */

    internal void* next_out; /** Pointer to the next output position. */
    internal IntPtr avail_out; /** Amount of free space in next_out. */
    internal ulong total_out; /** Total number of bytes written by liblzma. */

    /**
   * Custom memory allocation functions. Set to NULL to use
   * the standard malloc() and free().
   */
    private void* allocator;

    /** Internal state is not visible to applications. */
    private void* internal1;

    /*
   * Reserved space to allow possible future extensions without
   * breaking the ABI. Excluding the initialization of this structure,
   * you should not touch these, because the names of these variables
   * may change.
   */
    private void* reserved_ptr1;
    private void* reserved_ptr2;
    private void* reserved_ptr3;
    private void* reserved_ptr4;
    private ulong reserved_int1;
    private ulong reserved_int2;
    private IntPtr reserved_int3;
    private IntPtr reserved_int4;
    private uint reserved_enum1;
    private uint reserved_enum2;
  }

  public enum LZMACheck : uint
  {
    /**
     * No Check is calculated.
     *
     * Size of the Check field: 0 bytes
     */
    LZMA_CHECK_NONE = 0,


    /**
     * CRC32 using the polynomial from the IEEE 802.3 standard
     *
     * Size of the Check field: 4 bytes
     */
    LZMA_CHECK_CRC32 = 1,


    /**
     * CRC64 using the polynomial from the ECMA-182 standard
     *
     * Size of the Check field: 8 bytes
     */
    LZMA_CHECK_CRC64 = 4,

    /**
     * SHA-256
     *
     * Size of the Check field: 32 bytes
     */
    LZMA_CHECK_SHA256 = 10
  } ;
  /**
 * The `action' argument for lzma_code()
 *
 * After the first use of LZMA_SYNC_FLUSH, LZMA_FULL_FLUSH, or LZMA_FINISH,
 * the same `action' must is used until lzma_code() returns LZMA_STREAM_END.
 * Also, the amount of input (that is, strm->avail_in) must not be modified
 * by the application until lzma_code() returns LZMA_STREAM_END. Changing the
 * `action' or modifying the amount of input will make lzma_code() return
 * LZMA_PROG_ERROR.
 */

  public enum LZMAAction : uint
  {
    /**
     * Continue coding
     *
     * Encoder: Encode as much input as possible. Some internal
     * buffering will probably be done (depends on the filter
     * chain in use), which causes latency: the input used won't
     * usually be decodeable from the output of the same
     * lzma_code() call.
     *
     * Decoder: Decode as much input as possible and produce as
     * much output as possible.
     */
    LZMA_RUN = 0,

    /**
     * Make all the input available at output
     *
     * Normally the encoder introduces some latency.
     * LZMA_SYNC_FLUSH forces all the buffered data to be
     * available at output without resetting the internal
     * state of the encoder. This way it is possible to use
     * compressed stream for example for communication over
     * network.
     *
     * Only some filters support LZMA_SYNC_FLUSH. Trying to use
     * LZMA_SYNC_FLUSH with filters that don't support it will
     * make lzma_code() return LZMA_OPTIONS_ERROR. For example,
     * LZMA1 doesn't support LZMA_SYNC_FLUSH but LZMA2 does.
     *
     * Using LZMA_SYNC_FLUSH very often can dramatically reduce
     * the compression ratio. With some filters (for example,
     * LZMA2), finetuning the compression options may help
     * mitigate this problem significantly.
     *
     * Decoders don't support LZMA_SYNC_FLUSH.
     */
    LZMA_SYNC_FLUSH = 1,

    /**
     * Make all the input available at output
     *
     * Finish encoding of the current Block. All the input
     * data going to the current Block must have been given
     * to the encoder (the last bytes can still be pending in
     * next_in). Call lzma_code() with LZMA_FULL_FLUSH until
     * it returns LZMA_STREAM_END. Then continue normally with
     * LZMA_RUN or finish the Stream with LZMA_FINISH.
     *
     * This action is currently supported only by Stream encoder
     * and easy encoder (which uses Stream encoder). If there is
     * no unfinished Block, no empty Block is created.
     */
    LZMA_FULL_FLUSH = 2,

    /**
     * Finish the coding operation
     *
     * Finishes the coding operation. All the input data must
     * have been given to the encoder (the last bytes can still
     * be pending in next_in). Call lzma_code() with LZMA_FINISH
     * until it returns LZMA_STREAM_END. Once LZMA_FINISH has
     * been used, the amount of input must no longer be changed
     * by the application.
     *
     * When decoding, using LZMA_FINISH is optional unless the
     * LZMA_CONCATENATED flag was used when the decoder was
     * initialized. When LZMA_CONCATENATED was not used, the only
     * effect of LZMA_FINISH is that the amount of input must not
     * be changed just like in the encoder.
     */
    LZMA_FINISH = 3
  }

  public enum LZMAStatus : uint
  {
    /**
     * Operation completed successfully
     */

    LZMA_OK = 0,
    /**
     * End of stream was reached
     *
     * In encoder, LZMA_SYNC_FLUSH, LZMA_FULL_FLUSH, or
     * LZMA_FINISH was finished. In decoder, this indicates
     * that all the data was successfully decoded.
     *
     * In all cases, when LZMA_STREAM_END is returned, the last
     * output bytes should be picked from strm->next_out.
     */

    LZMA_STREAM_END = 1,
    /**
     * Input stream has no integrity check
     *
     * This return value can be returned only if the
     * LZMA_TELL_NO_CHECK flag was used when initializing
     * the decoder. LZMA_NO_CHECK is just a warning, and
     * the decoding can be continued normally.
     *
     * It is possible to call lzma_get_check() immediatelly after
     * lzma_code has returned LZMA_NO_CHECK. The result will
     * naturally be LZMA_CHECK_NONE, but the possibility to call
     * lzma_get_check() may be convenient in some applications.
     */

    LZMA_NO_CHECK = 2,

    /**
     * Cannot calculate the integrity check
     *
     * The usage of this return value is different in encoders
     * and decoders.
     *
     * Encoders can return this value only from the initialization
     * function. If initialization fails with this value, the
     * encoding cannot be done, because there's no way to produce
     * output with the correct integrity check.
     *
     * Decoders can return this value only from lzma_code() and
     * only if the LZMA_TELL_UNSUPPORTED_CHECK flag was used when
     * initializing the decoder. The decoding can still be
     * continued normally even if the check type is unsupported,
     * but naturally the check will not be validated, and possible
     * errors may go undetected.
     *
     * With decoder, it is possible to call lzma_get_check()
     * immediatelly after lzma_code() has returned
     * LZMA_UNSUPPORTED_CHECK. This way it is possible to find
     * out what the unsupported Check ID was.
     */
    LZMA_UNSUPPORTED_CHECK = 3,

    /**
     * Integrity check type is now available
     *
     * This value can be returned only by the lzma_code() function
     * and only if the decoder was initialized with the
     * LZMA_TELL_ANY_CHECK flag. LZMA_GET_CHECK tells the
     * application that it may now call lzma_get_check() to find
     * out the Check ID. This can be used, for example, to
     * implement a decoder that accepts only files that have
     * strong enough integrity check.
     */
    LZMA_GET_CHECK = 4,

    /**
     * Cannot allocate memory
     *
     * Memory allocation failed, or the size of the allocation
     * would be greater than SIZE_MAX.
     *
     * Due to internal implementation reasons, the coding cannot
     * be continued even if more memory were made available after
     * LZMA_MEM_ERROR.
     */
    LZMA_MEM_ERROR = 5,

    /**
     * Memory usage limit was reached
     *
     * Decoder would need more memory than allowed by the
     * specified memory usage limit. To continue decoding,
     * the memory usage limit has to be increased with
     * lzma_memlimit().
     */
    LZMA_MEMLIMIT_ERROR = 6,

    /**
     * File format not recognized
     *
     * The decoder did not recognize the input as supported file
     * format. This error can occur, for example, when trying to
     * decode .lzma format file with lzma_stream_decoder,
     * because lzma_stream_decoder accepts only the .xz format.
     */
    LZMA_FORMAT_ERROR = 7,

    /**
     * Invalid or unsupported options
     *
     * Invalid or unsupported options, for example
     *  - unsupported filter(s) or filter options; or
     *  - reserved bits set in headers (decoder only).
     *
     * Rebuilding liblzma with more features enabled, or
     * upgrading to a newer version of liblzma may help.
     */
    LZMA_OPTIONS_ERROR = 8,

    /**
     * Data is corrupt
     *
     * The usage of this return value is different in encoders
     * and decoders. In both encoder and decoder, the coding
     * cannot continue after this error.
     *
     * Encoders return this if size limits of the target file
     * format would be exceeded. These limits are huge, thus
     * getting this error from an encoder is mostly theoretical.
     * For example, the maximum compressed and uncompressed
     * size of a .xz Stream created with lzma_stream_encoder is
     * 2^63 - 1 bytes (one byte less than 8 EiB).
     *
     * Decoders return this error if the input data is corrupt.
     * This can mean, for example, invalid CRC32 in headers
     * or invalid check of uncompressed data.
     */
    LZMA_DATA_ERROR = 9,

    /**
     * No progress is possible
     *
     * This error code is returned when the coder cannot consume
     * any new input and produce any new output. The most common
     * reason for this error is that the input stream being
     * decoded is truncated or corrupt.
     *
     * This error is not fatal. Coding can be continued normally
     * by providing more input and/or more output space, if
     * possible.
     *
     * Typically the first call to lzma_code() that can do no
     * progress returns LZMA_OK instead of LZMA_BUF_ERROR. Only
     * the second consecutive call doing no progress will return
     * LZMA_BUF_ERROR. This is intentional.
     *
     * With zlib, Z_BUF_ERROR may be returned even if the
     * application is doing nothing wrong. The above hack
     * guarantees that liblzma never returns LZMA_BUF_ERROR
     * to properly written applications unless the input file
     * is truncated or corrupt. This should simplify the
     * applications a little.
     */
    LZMA_BUF_ERROR = 10,

    /**
     * Programming error
     *
     * This indicates that the arguments given to the function are
     * invalid or the internal state of the decoder is corrupt.
     *   - Function arguments are invalid or the structures
     *     pointed by the argument pointers are invalid
     *     e.g. if strm->next_out has been set to NULL and
     *     strm->avail_out > 0 when calling lzma_code().
     *   - lzma_* functions have been called in wrong order
     *     e.g. lzma_code() was called right after lzma_end().
     *   - If errors occur randomly, the reason might be flaky
     *     hardware.
     *
     * If you think that your code is correct, this error code
     * can be a sign of a bug in liblzma. See the documentation
     * how to report bugs.
     */
    LZMA_PROG_ERROR = 11,
  }

  public enum LZMAMode : uint
  {
    /**
     * Fast compression
     *
     * Fast mode is usually at its best when combined with
     * a hash chain match finder.
     */
    LZMA_MODE_FAST = 1,
    /**
     * Normal compression
     *
     * This is usually notably slower than fast mode. Use this
     * together with binary tree match finders to expose the
     * full potential of the LZMA encoder.
     */
    LZMA_MODE_NORMAL = 2
  }

  /**
 * \brief       Options for the Delta filter
 *
 * These options are needed by both encoder and decoder.
 */

  internal enum LZMADeltaType {
    LZMA_DELTA_TYPE_BYTE
  }
  #if AMD64
  [StructLayout(LayoutKind.Sequential, Pack = 8)]
#endif
#if I386
  [StructLayout(LayoutKind.Sequential, Pack = 4)]
#endif
  internal unsafe struct LZMAOptionsDelta
  {
    public const int LZMA_DELTA_DIST_MIN = 1;
    public const int LZMA_DELTA_DIST_MAX = 256;

    public LZMAOptionsDelta(uint delta) { 
      type = LZMADeltaType.LZMA_DELTA_TYPE_BYTE;
      dist = delta;
      reserved_int1 = reserved_int2 = reserved_int3 = reserved_int4 = 0;
      reserved_ptr1 = reserved_ptr2 = null;
    }

    /** For now, this must always be LZMA_DELTA_TYPE_BYTE. */
    private LZMADeltaType type;

    /**
     * \brief       Delta distance
     *
     * With the only currently supported type, LZMA_DELTA_TYPE_BYTE,
     * the distance is as bytes.
     *
     * Examples:
     *  - 16-bit stereo audio: distance = 4 bytes
     *  - 24-bit RGB image data: distance = 3 bytes
     */
    private uint dist;

    /*
     * Reserved space to allow possible future extensions without
     * breaking the ABI. You should not touch these, because the names
     * of these variables may change. These are and will never be used
     * when type is LZMA_DELTA_TYPE_BYTE, so it is safe to leave these
     * uninitialized.
     */
    private uint reserved_int1;
    private uint reserved_int2;
    private uint reserved_int3;
    private uint reserved_int4;
    private void* reserved_ptr1;
    private void* reserved_ptr2;

  }


  /**
 * \brief       Match finders
 *
 * Match finder has major effect on both speed and compression ratio.
 * Usually hash chains are faster than binary trees.
 *
 * The memory usage formulas are only rough estimates, which are closest to
 * reality when dict_size is a power of two. The formulas are  more complex
 * in reality, and can also change a little between liblzma versions. Use
 * lzma_memusage_encoder() to get more accurate estimate of memory usage.
 */



  public enum LZMAMatchFinder : uint
  {
    /**
     * Hash Chain with 2- and 3-byte hashing
     *
     * Minimum nice_len: 3
     *
     * Memory usage:
     *  - dict_size <= 16 MiB: dict_size * 7.5
     *  - dict_size > 16 MiB: dict_size * 5.5 + 64 MiB
     */
    LZMA_MF_HC3 = 0x03,


    /**
     * Hash Chain with 2-, 3-, and 4-byte hashing
     *
     * Minimum nice_len: 4
     *
     * Memory usage: dict_size * 7.5
     */
    LZMA_MF_HC4 = 0x04,

    /**
     * Binary Tree with 2-byte hashing
     *
     * Minimum nice_len: 2
     *
     * Memory usage: dict_size * 9.5
     */
    LZMA_MF_BT2 = 0x12,

    /**
     * Binary Tree with 2- and 3-byte hashing
     *
     * Minimum nice_len: 3
     *
     * Memory usage:
     *  - dict_size <= 16 MiB: dict_size * 11.5
     *  - dict_size > 16 MiB: dict_size * 9.5 + 64 MiB
     */
    LZMA_MF_BT3 = 0x13,

    /**
     * Binary Tree with 2-, 3-, and 4-byte hashing
     *
     * Minimum nice_len: 4
     *
     * Memory usage: dict_size * 11.5
     */
    LZMA_MF_BT4 = 0x14
  }

#if AMD64
  [StructLayout(LayoutKind.Sequential, Pack = 8)]
#endif
#if I386
  [StructLayout(LayoutKind.Sequential, Pack = 4)]
#endif
  public unsafe struct LZMAOptionLZMA
  {
    public LZMAOptionLZMA(uint preset)
    {
      fixed (LZMAOptionLZMA *ptr = &this) {
        LZMANative.lzma_lzma_preset(ptr, preset);
      }
    }

    /**
   * \brief       Dictionary size in bytes
   *
   * Dictionary size indicates how many bytes of the recently processed
   * uncompressed data is kept in memory. One method to reduce size of
   * the uncompressed data is to store distance-length pairs, which
   * indicate what data to repeat from the dictionary buffer. Thus,
   * the bigger the dictionary, the better compression ratio usually is.
   *
   * Maximum size of the dictionary depends on multiple things:
   *  - Memory usage limit
   *  - Available address space (not a problem on 64-bit systems)
   *  - Selected match finder (encoder only)
   *
   * Currently the maximum dictionary size for encoding is 1.5 GiB
   * (i.e. (UINT32_C(1) << 30) + (UINT32_C(1) << 29)) even on 64-bit
   * systems for certain match finder implementation reasons. In future,
   * there may be match finders that support bigger dictionaries (3 GiB
   * will probably be the maximum).
   *
   * Decoder already supports dictionaries up to 4 GiB - 1 B (i.e.
   * UINT32_MAX), so increasing the maximum dictionary size of the
   * encoder won't cause problems for old decoders.
   *
   * Because extremely small dictionaries sizes would have unneeded
   * overhead in the decoder, the minimum dictionary size is 4096 bytes.
   *
   * \note        When decoding, too big dictionary does no other harm
   *              than wasting memory.
   */
    private uint dict_size;

    /**
   * \brief       Pointer to an initial dictionary
   *
   * It is possible to initialize the LZ77 history window using
   * a preset dictionary. Here is a good quote from zlib's
   * documentation; this applies to LZMA as is:
   *
   * "The dictionary should consist of strings (byte sequences) that
   * are likely to be encountered later in the data to be compressed,
   * with the most commonly used strings preferably put towards the
   * end of the dictionary. Using a dictionary is most useful when
   * the data to be compressed is short and can be predicted with
   * good accuracy; the data can then be compressed better than
   * with the default empty dictionary."
   * (From deflateSetDictionary() in zlib.h of zlib version 1.2.3)
   *
   * This feature should be used only in special situations.
   * It works correctly only with raw encoding and decoding.
   * Currently none of the container formats supported by
   * liblzma allow preset dictionary when decoding, thus if
   * you create a .lzma file with preset dictionary, it cannot
   * be decoded with the regular .lzma decoder functions.
   *
   * \todo        This feature is not implemented yet.
   */
    public void* preset_dict;

    /**
   * \brief       Size of the preset dictionary
   *
   * Specifies the size of the preset dictionary. If the size is
   * bigger than dict_size, only the last dict_size bytes are processed.
   *
   * This variable is read only when preset_dict is not NULL.
   */
    public uint preset_dict_size;

    /**
   * \brief       Number of literal context bits
   *
   * How many of the highest bits of the previous uncompressed
   * eight-bit byte (also known as `literal') are taken into
   * account when predicting the bits of the next literal.
   *
   * \todo        Example
   *
   * There is a limit that applies to literal context bits and literal
   * position bits together: lc + lp <= 4. Without this limit the
   * decoding could become very slow, which could have security related
   * results in some cases like email servers doing virus scanning.
   * This limit also simplifies the internal implementation in liblzma.
   *
   * There may be LZMA streams that have lc + lp > 4 (maximum lc
   * possible would be 8). It is not possible to decode such streams
   * with liblzma.
   */
    private uint lc;

    /**
   * \brief       Number of literal position bits
   *
   * How many of the lowest bits of the current position (number
   * of bytes from the beginning of the uncompressed data) in the
   * uncompressed data is taken into account when predicting the
   * bits of the next literal (a single eight-bit byte).
   *
   * \todo        Example
   */
    private uint lp;

    /**
   * \brief       Number of position bits
   *
   * How many of the lowest bits of the current position in the
   * uncompressed data is taken into account when estimating
   * probabilities of matches. A match is a sequence of bytes for
   * which a matching sequence is found from the dictionary and
   * thus can be stored as distance-length pair.
   *
   * Example: If most of the matches occur at byte positions of
   * 8 * n + 3, that is, 3, 11, 19, ... set pb to 3, because 2**3 == 8.
   */
    private uint pb;

    /** LZMA compression mode */
    public LZMAMode mode;

    /**
   * \brief       Nice length of a match
   *
   * This determines how many bytes the encoder compares from the match
   * candidates when looking for the best match. Once a match of at
   * least nice_len bytes long is found, the encoder stops looking for
   * better condidates and encodes the match. (Naturally, if the found
   * match is actually longer than nice_len, the actual length is
   * encoded; it's not truncated to nice_len.)
   *
   * Bigger values usually increase the compression ratio and
   * compression time. For most files, 30 to 100 is a good value,
   * which gives very good compression ratio at good speed.
   *
   * The exact minimum value depends on the match finder. The maximum is
   * 273, which is the maximum length of a match that LZMA can encode.
   */
    private uint nice_len;

    /** Match finder ID */
    private LZMAMatchFinder mf;

    /**
   * \brief       Maximum search depth in the match finder
   *
   * For every input byte, match finder searches through the hash chain
   * or binary tree in a loop, each iteration going one step deeper in
   * the chain or tree. The searching stops if
   *  - a match of at least nice_len bytes long is found;
   *  - all match candidates from the hash chain or binary tree have
   *    been checked; or
   *  - maximum search depth is reached.
   *
   * Maximum search depth is needed to prevent the match finder from
   * wasting too much time in case there are lots of short match
   * candidates. On the other hand, stopping the search before all
   * candidates have been checked can reduce compression ratio.
   *
   * Setting depth to zero tells liblzma to use an automatic default
   * value, that depends on the selected match finder and nice_len.
   * The default is in the range [10, 200] or so (it may vary between
   * liblzma versions).
   *
   * Using a bigger depth value than the default can increase
   * compression ratio in some cases. There is no strict maximum value,
   * but high values (thousands or millions) should be used with care:
   * the encoder could remain fast enough with typical input, but
   * malicious input could cause the match finder to slow down
   * dramatically, possibly creating a denial of service attack.
   */
    private uint depth;

    /*
    * Reserved space to allow possible future extensions without
    * breaking the ABI. You should not touch these, because the names
    * of these variables may change. These are and will never be used
    * with the currently supported options, so it is safe to leave these
    * uninitialized.
    */
    private uint reserved_int1;
    private uint reserved_int2;
    private uint reserved_int3;
    private uint reserved_int4;
    private uint reserved_int5;
    private uint reserved_int6;
    private uint reserved_int7;
    private uint reserved_int8;
    private uint reserved_enum1;
    private uint reserved_enum2;
    private uint reserved_enum3;
    private uint reserved_enum4;
    private void* reserved_ptr1;
    private void* reserved_ptr2;

    public uint LiteralPositionBits
    {
      get { return lp; }
      set { lp = value; }
    }

    public uint PositionBits
    {
      get { return pb; }
      set { pb = value; }
    }

    public uint LiteralContextBits
    {
      get { return lc; }
      set { lc = value; }
    }

    public LZMAMatchFinder MatchFinder
    {
      get { return mf; }
      set { mf = value; }
    }

    public uint Depth
    {
      get { return depth; }
      set { depth = value; }
    }

    public uint DictionarySize
    {
      get { return dict_size; }
      set { dict_size = value; }
    }

    public uint NiceMatchLength
    {
      get { return nice_len; }
      set { nice_len = value; }
    }
  }
}
