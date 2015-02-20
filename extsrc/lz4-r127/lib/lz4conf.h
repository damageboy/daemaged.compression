#ifndef LZCONF_H
#define LZCONF_H

#if defined(WINDOWS) || defined(WIN32)
   /* If building or using as a DLL, define LZ4_DLL.
    * This is not mandatory, but it offers a little performance increase.
    */
#  ifdef LZ4_DLL
#    if defined(WIN32)
#      ifdef LZ4_INTERNAL
#        define LZ4_EXTERN extern __declspec(dllexport)
#      else
#        define LZ4_EXTERN extern __declspec(dllimport)
#      endif
#    endif
#  endif  /* LZ4_DLL */
#endif

#ifndef LZ4_EXTERN
#  define LZ4_EXTERN extern
#endif


#endif /* LZCONF_H */
