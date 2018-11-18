#pragma once

#include "profilerbase.hpp"

class ProfilerImpl :
	public ProfilerBase
{
public:
	ProfilerImpl();
	~ProfilerImpl();

	STDMETHOD(QueryInterface)(REFIID riid, void **ppObj);
	ULONG STDMETHODCALLTYPE AddRef();
	ULONG STDMETHODCALLTYPE Release();
	STDMETHOD(Initialize)(IUnknown *pICorProfilerInfoUnk);

	STDMETHOD(ModuleLoadFinished)(ModuleID moduleID, HRESULT hrStatus);

private:
	HRESULT SetProfilerEventMask();

private:
	ICorProfilerInfo2* mCorProfilerInfo2 = nullptr;

	long mRefCount;
};
