#include "profilerfactory.hpp"
#include "profilerimpl.hpp"

STDMETHODIMP ProfilerFactory::QueryInterface(REFIID riid, void **ppvObject) {
	*ppvObject = nullptr;

	if (IsEqualIID(riid, IID_IUnknown) || IsEqualIID(riid, IID_IClassFactory))
		*ppvObject = static_cast<IClassFactory *>(this);
	else
		return E_NOINTERFACE;

	AddRef();
	return S_OK;
}

STDMETHODIMP_(ULONG) ProfilerFactory::AddRef() {
	LockServerCount(TRUE);
	return 2;
}

STDMETHODIMP_(ULONG) ProfilerFactory::Release() {
	LockServerCount(FALSE);
	return 1;
}

STDMETHODIMP ProfilerFactory::CreateInstance(IUnknown *pUnkOuter, REFIID riid, void **ppvObject) {
	*ppvObject = nullptr;
	if (pUnkOuter != nullptr)
		return CLASS_E_NOAGGREGATION;

	Profiler* p = new ProfilerImpl();
	if (p == nullptr) return E_OUTOFMEMORY;

	auto hr = p->QueryInterface(riid, ppvObject);
	p->Release();

	return hr;
}

STDMETHODIMP ProfilerFactory::LockServer(BOOL fLock) {
	LockServerCount(fLock);
	return S_OK;
}

static long g_lockServerCount = 0;

void ProfilerFactory::LockServerCount(BOOL bLock) {
	if (bLock) {
		InterlockedIncrement(&g_lockServerCount);
	}
	else {
		InterlockedDecrement(&g_lockServerCount);
	}
}

bool ProfilerFactory::DllCanUnloadNow() {
	return g_lockServerCount == 0 ? S_OK : S_FALSE;
}
