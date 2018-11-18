#include "profilerimpl.hpp"
#include "profilerfactory.hpp"

#include <windows.h>
#include <objbase.h>
#include <stdio.h>

#include <iostream>

HMODULE g_hModule = nullptr;


BOOL APIENTRY DllMain(HANDLE hModule, DWORD dwReason, void* lpReserved)
{
	if (dwReason == DLL_PROCESS_ATTACH)
		g_hModule = (HMODULE)hModule;
	return TRUE;
}

STDAPI DllGetClassObject(const CLSID& clsid, const IID& iid, void** ppv)
{
	static ProfilerFactory factory;

	*ppv = nullptr;
	if (IsEqualCLSID(clsid, __uuidof(ProfilerImpl)))
	{
		return factory.QueryInterface(iid, ppv);
	}

	return CLASS_E_CLASSNOTAVAILABLE;
}

STDAPI DllCanUnloadNow()
{
	return ProfilerFactory::DllCanUnloadNow();
}
