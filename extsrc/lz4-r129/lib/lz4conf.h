#ifndef LZCONF_H
#define LZCONF_H

   /* If building or using as a DLL, define LZ4_DLL.
    * This is not mandatory, but it offers a little performance increase.
    */
#ifdef WIN32
#  ifdef LZ4_DLL
#    define LZ4_EXTERN extern __declspec(dllexport)
#  else
#    define LZ4_EXTERN extern __declspec(dllimport)
#  endif /* LZ4_DLL */
#endif /* WIN32 */

#ifndef LZ4_EXTERN
#  define LZ4_EXTERN extern
#endif

#endif /* LZCONF_H */
