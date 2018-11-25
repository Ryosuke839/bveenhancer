

/* this ALWAYS GENERATED file contains the definitions for the interfaces */


 /* File created by MIDL compiler version 8.00.0603 */
/* at Tue Nov 20 23:56:24 2018
 */
/* Compiler settings for profiler.idl:
    Oicf, W1, Zp8, env=Win32 (32b run), target_arch=X86 8.00.0603 
    protocol : dce , ms_ext, c_ext, robust
    error checks: allocation ref bounds_check enum stub_data 
    VC __declspec() decoration level: 
         __declspec(uuid()), __declspec(selectany), __declspec(novtable)
         DECLSPEC_UUID(), MIDL_INTERFACE()
*/
/* @@MIDL_FILE_HEADING(  ) */

#pragma warning( disable: 4049 )  /* more than 64k source lines */


/* verify that the <rpcndr.h> version is high enough to compile this file*/
#ifndef __REQUIRED_RPCNDR_H_VERSION__
#define __REQUIRED_RPCNDR_H_VERSION__ 475
#endif

#include "rpc.h"
#include "rpcndr.h"

#ifndef __RPCNDR_H_VERSION__
#error this stub requires an updated version of <rpcndr.h>
#endif // __RPCNDR_H_VERSION__

#ifndef COM_NO_WINDOWS_H
#include "windows.h"
#include "ole2.h"
#endif /*COM_NO_WINDOWS_H*/

#ifndef __profiler_h_h__
#define __profiler_h_h__

#if defined(_MSC_VER) && (_MSC_VER >= 1020)
#pragma once
#endif

/* Forward Declarations */ 

#ifndef __Profiler_FWD_DEFINED__
#define __Profiler_FWD_DEFINED__
typedef interface Profiler Profiler;

#endif 	/* __Profiler_FWD_DEFINED__ */


#ifndef __ProfilerImpl_FWD_DEFINED__
#define __ProfilerImpl_FWD_DEFINED__

#ifdef __cplusplus
typedef class ProfilerImpl ProfilerImpl;
#else
typedef struct ProfilerImpl ProfilerImpl;
#endif /* __cplusplus */

#endif 	/* __ProfilerImpl_FWD_DEFINED__ */


/* header files for imported files */
#include "oaidl.h"
#include "ocidl.h"

#ifdef __cplusplus
extern "C"{
#endif 


#ifndef __Profiler_INTERFACE_DEFINED__
#define __Profiler_INTERFACE_DEFINED__

/* interface Profiler */
/* [oleautomation][uuid][object] */ 


EXTERN_C const IID IID_Profiler;

#if defined(__cplusplus) && !defined(CINTERFACE)
    
    MIDL_INTERFACE("AFEE96F0-F815-427D-952B-1BF01A0A7BC9")
    Profiler : public IUnknown
    {
    public:
    };
    
    
#else 	/* C style interface */

    typedef struct ProfilerVtbl
    {
        BEGIN_INTERFACE
        
        HRESULT ( STDMETHODCALLTYPE *QueryInterface )( 
            Profiler * This,
            /* [in] */ REFIID riid,
            /* [annotation][iid_is][out] */ 
            _COM_Outptr_  void **ppvObject);
        
        ULONG ( STDMETHODCALLTYPE *AddRef )( 
            Profiler * This);
        
        ULONG ( STDMETHODCALLTYPE *Release )( 
            Profiler * This);
        
        END_INTERFACE
    } ProfilerVtbl;

    interface Profiler
    {
        CONST_VTBL struct ProfilerVtbl *lpVtbl;
    };

    

#ifdef COBJMACROS


#define Profiler_QueryInterface(This,riid,ppvObject)	\
    ( (This)->lpVtbl -> QueryInterface(This,riid,ppvObject) ) 

#define Profiler_AddRef(This)	\
    ( (This)->lpVtbl -> AddRef(This) ) 

#define Profiler_Release(This)	\
    ( (This)->lpVtbl -> Release(This) ) 


#endif /* COBJMACROS */


#endif 	/* C style interface */




#endif 	/* __Profiler_INTERFACE_DEFINED__ */



#ifndef __ProfilerLib_LIBRARY_DEFINED__
#define __ProfilerLib_LIBRARY_DEFINED__

/* library ProfilerLib */
/* [version][uuid] */ 


EXTERN_C const IID LIBID_ProfilerLib;

EXTERN_C const CLSID CLSID_ProfilerImpl;

#ifdef __cplusplus

class DECLSPEC_UUID("7ADA6F81-2F62-4432-8BA0-C18CECAE1546")
ProfilerImpl;
#endif
#endif /* __ProfilerLib_LIBRARY_DEFINED__ */

/* Additional Prototypes for ALL interfaces */

/* end of Additional Prototypes */

#ifdef __cplusplus
}
#endif

#endif


