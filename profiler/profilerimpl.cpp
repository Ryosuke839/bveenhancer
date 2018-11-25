#define NOMINMAX
#include "profilerimpl.hpp"
#include <corhdr.h>

#include <set>
#include <map>
#include <vector>
#include <fstream>
#include <string>
#include <sstream>
#include <memory>
#include <iostream>
#include <algorithm>

#pragma comment (lib, "corguids.lib")

ProfilerImpl::ProfilerImpl() {
	mRefCount = 1;
}

ProfilerImpl::~ProfilerImpl() {
}

HRESULT ProfilerImpl::SetProfilerEventMask() {
	DWORD eventMask = 0;
	eventMask |= COR_PRF_MONITOR_MODULE_LOADS;

	return mCorProfilerInfo2->SetEventMask(eventMask);
}

STDMETHODIMP ProfilerImpl::QueryInterface(REFIID riid, void **ppObj) {
	*ppObj = nullptr;

	if (riid == IID_IUnknown) {
		*ppObj = static_cast<IUnknown *>(static_cast<Profiler *>(this));
		AddRef();
		return S_OK;
	}

	if (riid == __uuidof(Profiler)) {
		*ppObj = static_cast<Profiler *>(this);
		AddRef();
		return S_OK;
	}

	if (riid == IID_ICorProfilerCallback) {
		*ppObj = static_cast<ICorProfilerCallback*>(this);
		AddRef();
		return S_OK;
	}

	if (riid == IID_ICorProfilerCallback2) {
		*ppObj = static_cast<ICorProfilerCallback2*>(this);
		AddRef();
		return S_OK;
	}

	if (riid == IID_ICorProfilerCallback3) {
		*ppObj = dynamic_cast<ICorProfilerCallback3*>(this);
		AddRef();
		return S_OK;
	}

	if (riid == IID_ICorProfilerInfo || riid == IID_ICorProfilerInfo2) {
		mCorProfilerInfo2->QueryInterface(riid, ppObj);
		return S_OK;
	}

	return E_NOINTERFACE;
}

ULONG STDMETHODCALLTYPE ProfilerImpl::AddRef() {
	return InterlockedIncrement(&mRefCount);
}

ULONG STDMETHODCALLTYPE ProfilerImpl::Release() {
	long nRefCount = InterlockedDecrement(&mRefCount);
	if (nRefCount == 0) {
		if (mCorProfilerInfo2)
			mCorProfilerInfo2->Release();
		delete this;
	}
	return nRefCount;
}

STDMETHODIMP ProfilerImpl::Initialize(IUnknown *pICorProfilerInfoUnk) {

	pICorProfilerInfoUnk->QueryInterface(__uuidof(mCorProfilerInfo2), (LPVOID*)&mCorProfilerInfo2);

	SetProfilerEventMask();

	auto info = mCorProfilerInfo2;

	return S_OK;
}

static PCCOR_SIGNATURE ParseSignature(PCCOR_SIGNATURE sig)
{
	switch (*sig++) {
	case ELEMENT_TYPE_VOID:
	case ELEMENT_TYPE_BOOLEAN:
	case ELEMENT_TYPE_CHAR:
	case ELEMENT_TYPE_I1:
	case ELEMENT_TYPE_U1:
	case ELEMENT_TYPE_I2:
	case ELEMENT_TYPE_U2:
	case ELEMENT_TYPE_I4:
	case ELEMENT_TYPE_U4:
	case ELEMENT_TYPE_I8:
	case ELEMENT_TYPE_U8:
	case ELEMENT_TYPE_R4:
	case ELEMENT_TYPE_R8:
	case ELEMENT_TYPE_STRING:
	case ELEMENT_TYPE_VAR:
	case ELEMENT_TYPE_MVAR:
	case ELEMENT_TYPE_TYPEDBYREF:
	case ELEMENT_TYPE_I:
	case ELEMENT_TYPE_U:
	case ELEMENT_TYPE_OBJECT:
		break;
	case ELEMENT_TYPE_SZARRAY:
	case ELEMENT_TYPE_PINNED:
	case ELEMENT_TYPE_PTR:
	case ELEMENT_TYPE_BYREF:
		sig = ParseSignature(sig);
		break;
	case ELEMENT_TYPE_VALUETYPE:
	case ELEMENT_TYPE_CLASS:
	case ELEMENT_TYPE_CMOD_REQD:
	case ELEMENT_TYPE_CMOD_OPT:
		CorSigUncompressToken(sig);
		break;
	case ELEMENT_TYPE_GENERICINST:
		sig = ParseSignature(sig);
		for (ULONG i = 0, n = CorSigUncompressData(sig); i < n; ++i)
			sig = ParseSignature(sig);
		break;
	case ELEMENT_TYPE_ARRAY:
		sig = ParseSignature(sig);
		if (CorSigUncompressData(sig))
		{
			for (ULONG i = 0, n = CorSigUncompressData(sig); i < n; i++)
				CorSigUncompressData(sig);
			for (ULONG i = 0, n = CorSigUncompressData(sig); i < n; i++)
				CorSigUncompressData(sig);
		}
		break;
	default:
	case ELEMENT_TYPE_END:
	case ELEMENT_TYPE_SENTINEL:
		break;
	}

	return sig;
}

extern HMODULE g_hModule;

STDMETHODIMP ProfilerImpl::ModuleLoadFinished(ModuleID moduleID, HRESULT hrStatus) try
{
	auto info = mCorProfilerInfo2;

	WCHAR moduleName[2048];
	AssemblyID assemblyID;
	info->GetModuleInfo(moduleID, NULL, 2048, 0, moduleName, &assemblyID);

	WCHAR moduleName2[2048];
	GetModuleFileName(nullptr, moduleName2, 2048);

	if (wcscmp(moduleName, moduleName2) != 0)
		return S_OK;

	IMetaDataImport* import;
	auto hr = info->GetModuleMetaData(moduleID, ofRead, IID_IMetaDataImport, (IUnknown**)&import);
	if (FAILED(hr))
		throw L"Failed GetModuleMetaData @" + std::to_wstring(hr);

	IMetaDataEmit2* metaDataEmit;
	hr = info->GetModuleMetaData(moduleID, ofRead | ofWrite, IID_IMetaDataEmit2, (IUnknown**)&metaDataEmit);
	if (FAILED(hr))
		throw L"Failed metaDataEmit @" + std::to_wstring(hr);

	IMetaDataAssemblyEmit* metaDataAssemblyEmit;
	hr = metaDataEmit->QueryInterface(IID_IMetaDataAssemblyEmit, (void**)&metaDataAssemblyEmit);
	if (FAILED(hr))
		throw L"Failed QueryInterface @" + std::to_wstring(hr);

	IMethodMalloc* methodMalloc;
	hr = info->GetILFunctionBodyAllocator(moduleID, &methodMalloc);
	if (FAILED(hr))
		throw L"Failed QueryInterface @" + std::to_wstring(hr);

	IMetaDataAssemblyImport* metaDataAssemblyImport;
	hr = import->QueryInterface(IID_IMetaDataAssemblyImport, (void**)&metaDataAssemblyImport);
	if (FAILED(hr))
		throw L"Failed QueryInterface @" + std::to_wstring(hr);

	mdAssembly assembly;
	hr = metaDataAssemblyImport->GetAssemblyFromScope(&assembly);
	if (FAILED(hr))
		throw L"Failed GetAssemblyFromScope @" + std::to_wstring(hr);

	ASSEMBLYMETADATA asmMeta;
	DWORD flags;
	hr = metaDataAssemblyImport->GetAssemblyProps(assembly, nullptr, nullptr, nullptr, nullptr, 0, nullptr, &asmMeta, &flags);
	if (FAILED(hr))
		throw L"Failed GetAssemblyFromScope @" + std::to_wstring(hr);

	std::wstring version =
		std::to_wstring(asmMeta.usMajorVersion) + L'.' +
		std::to_wstring(asmMeta.usMinorVersion) + L'.' +
		std::to_wstring(asmMeta.usBuildNumber) + L'.' +
		std::to_wstring(asmMeta.usRevisionNumber);

	HANDLE hFind;
	WIN32_FIND_DATA win32fd;//defined at Windwos.h
	hFind = FindFirstFile(L"*.bed", &win32fd);

	if (hFind != INVALID_HANDLE_VALUE)
	{
		do
		{
			if (win32fd.dwFileAttributes & FILE_ATTRIBUTE_DIRECTORY)
				continue;

			std::wstring filename(win32fd.cFileName);
			std::wifstream ifs(filename);

			std::wstring asmname = filename.substr(0, filename.rfind(L'.'));

			std::set<std::wstring> versions;
			bool inproc = false;
			mdTypeRef typeRef = mdTypeRefNil;

			std::wstring line;
			int lineno = 1;
			for (std::getline(ifs, line); !ifs.eof(); std::getline(ifs, line))
			{
				try
				{
					++lineno;
					if (line.find_first_of(L";#") != std::wstring::npos)
						line = line.substr(0, line.find_first_of(L";#"));
					if (line.size())
						if (line.find_last_not_of(L" \t") != std::wstring::npos)
							line = line.substr(0, line.find_last_not_of(L" \t") + 1);
						else
							line.clear();
					if (line.size() < 1)
						continue;
					if (line[0] == L'[')
					{
						if (line.find(L']', 1) == std::wstring::npos)
							throw std::wstring(L"Expected ] but not found");
						auto ver = line.substr(1, line.find(L']', 1) - 1);
						versions.insert(ver);

						inproc = version == ver;

						if (inproc && typeRef == mdTypeRefNil)
						{
							mdAssemblyRef assemblyRef = mdAssemblyRefNil;
							ASSEMBLYMETADATA assemblyMetaData = { 0 };
							assemblyMetaData.usMajorVersion = 1; //assembly version is 1.0.0.0
							auto hr = metaDataAssemblyEmit->DefineAssemblyRef(nullptr, 0, asmname.c_str(), &assemblyMetaData, nullptr, 0, 0, &assemblyRef);
							if (FAILED(hr))
								throw L"Failed DefineAssemblyRef " + asmname + L".Hook" + L"@" + std::to_wstring(hr);
							hr = metaDataEmit->DefineTypeRefByName(assemblyRef, (asmname + L".Hook").c_str(), &typeRef);
							if (FAILED(hr))
								throw L"Failed DefineTypeRefByName " + asmname + L".Hook" + L"@" + std::to_wstring(hr);
						}

						continue;
					}
					if (inproc)
					{
						std::wistringstream ss(line);
						std::vector<std::wstring> args;
						while (!ss.eof())
						{
							std::wstring arg;
							ss >> arg;
							args.push_back(arg);
						}

						if (args.size() < 1)
							throw std::wstring(L"Too few arguments");

						if (args[0] == L"Hook")
						{
							if (args.size() < 4)
								throw std::wstring(L"Too few arguments for type Hook");

							const auto token = std::wcstoul(args[1].c_str(), nullptr, 16);
							unsigned long replacetoken = token;

							const std::set<std::wstring> typelist = { L"REPLACE", L"BEFORE", L"AFTER_RET", L"AFTER_NORET", L"INJECT", L"PROXY" };
							if (!typelist.count(args[2]))
								throw L"Unknown hook type " + args[2];

							LPCBYTE p;
							ULONG s;
							hr = info->GetILFunctionBody(moduleID, token, &p, &s);
							if (FAILED(hr))
								throw L"Failed GetILFunctionBody for " + std::to_wstring(token);

							std::pair<size_t, size_t> injectpos(0, 0);
							std::vector<std::wstring> argList;
							std::wstring methodName;
							if (args[2] == L"INJECT")
							{
								if (args.size() < 6)
									throw std::wstring(L"Too few arguments for hook type INJECT");

								injectpos.first = std::wcstoul(args[3].c_str(), nullptr, 16);
								injectpos.second = std::wcstoul(args[4].c_str(), nullptr, 16);

								if (injectpos.first > injectpos.second)
									throw std::wstring(L"Invalid INJECT range");

								if (((p[0] & 7) == 3 ? 12 : 1) + injectpos.second > s)
									throw std::wstring(L"Too long INJECT range, method body length is ") + std::to_wstring(s - ((p[0] & 7) == 3 ? 12 : 1));

								methodName = args[5];
								argList = std::vector<std::wstring>(args.begin() + 6, args.end());
							} else
							if (args[2] == L"PROXY")
							{
								if (args.size() != 5)
									throw std::wstring(L"Invalid number of arguments for hook type PROXY");

								injectpos.first = std::wcstoul(args[3].c_str(), nullptr, 16);
								injectpos.second = injectpos.first + 4;

								if (((p[0] & 7) == 3 ? 12 : 1) + injectpos.second > s)
									throw std::wstring(L"Too long PROXY range, method body length is ") + std::to_wstring(s - ((p[0] & 7) == 3 ? 12 : 1));

								replacetoken =
									(static_cast<unsigned long>(p[((p[0] & 7) == 3 ? 12 : 1) + injectpos.first + 0]) << 0) |
									(static_cast<unsigned long>(p[((p[0] & 7) == 3 ? 12 : 1) + injectpos.first + 1]) << 8) |
									(static_cast<unsigned long>(p[((p[0] & 7) == 3 ? 12 : 1) + injectpos.first + 2]) << 16) |
									(static_cast<unsigned long>(p[((p[0] & 7) == 3 ? 12 : 1) + injectpos.first + 3]) << 24);

								methodName = args[4];
								argList = std::vector<std::wstring>(args.begin() + 5, args.end());
							} else
							{
								methodName = args[3];
								argList = std::vector<std::wstring>(args.begin() + 4, args.end());
							}

							mdTypeDef clstoken;
							PCCOR_SIGNATURE oldsig;
							ULONG oldsiglen;
							ULONG oldrva;
							DWORD attrflags;
							DWORD implflags;
							if ((replacetoken & 0xFF000000) != 0x0A000000)
							{
								auto hr = import->GetMethodProps(replacetoken, &clstoken, nullptr, 0, nullptr, &attrflags, &oldsig, &oldsiglen, &oldrva, &implflags);
								if (FAILED(hr))
									throw L"Failed GetMethodProps for " + std::to_wstring(token);
							} else
							{
								auto hr = import->GetMemberRefProps(replacetoken, &clstoken, nullptr, 0, nullptr, &oldsig, &oldsiglen);
								if (FAILED(hr))
									throw L"Failed GetMemberRefProps for " + std::to_wstring(token);
							}

							mdMethodDef method = mdMethodDefNil;
							if (args[2] != L"REPLACE" && args[2] != L"PROXY")
							{
								hr = metaDataEmit->DefineMethod(clstoken, (methodName + L"_org").c_str(), attrflags & ~mdSpecialName & ~mdRTSpecialName, oldsig, oldsiglen, oldrva, implflags, &method);
								if (FAILED(hr))
									throw L"Failed DefineMethod " + methodName + L"_org" + L" for " + std::to_wstring(clstoken);
							}

							COR_SIGNATURE convention = *oldsig;

							size_t generics = 0;
							size_t genericidx = 0;
							for (const auto& arg : argList)
								if (arg[0] == L'<')
									++generics;

							std::vector<uint8_t> il(p, p + ((p[0] & 7) == 3 ? 12 : 1) + injectpos.first);
							std::vector<uint8_t> sig = { static_cast<uint8_t>(generics ? IMAGE_CEE_CS_CALLCONV_GENERIC : IMAGE_CEE_CS_CALLCONV_DEFAULT) };
							std::vector<uint8_t> inst = { IMAGE_CEE_CS_CALLCONV_GENERICINST };

							// argsize
							if (generics)
							{
								sig.resize(sig.size() + 4);
								sig.resize(sig.size() - 4 + CorSigCompressData(generics, &sig[sig.size() - 4]));
								inst.resize(inst.size() + 4);
								inst.resize(inst.size() - 4 + CorSigCompressData(generics, &inst[inst.size() - 4]));
							}

							std::vector<std::pair<PCCOR_SIGNATURE, PCCOR_SIGNATURE>> arglist(CorSigUncompressData(++oldsig));
							sig.resize(sig.size() + 4);
							sig.resize(sig.size() - 4 + CorSigCompressData(args[2] != L"PROXY" ? argList.size() : arglist.size() + (convention & IMAGE_CEE_CS_CALLCONV_HASTHIS ? 1 : 0), &sig[sig.size() - 4]));

							std::vector<uint8_t> clssig = { ELEMENT_TYPE_CLASS, 0, 0, 0, 0 };
							clssig.resize(1 + CorSigCompressToken(clstoken, &clssig[1]));

							COR_SIGNATURE returnsig = *oldsig;
							// return type
							if (args[2] == L"BEFORE" || args[2] == L"AFTER_NORET" || args[2] == L"INJECT")
								sig.push_back(ELEMENT_TYPE_VOID);
							else
								sig.insert(sig.end(), oldsig, ParseSignature(oldsig));
							oldsig = ParseSignature(oldsig);

							if (args[2] == L"PROXY" && convention & IMAGE_CEE_CS_CALLCONV_HASTHIS)
								sig.insert(sig.end(), clssig.data(), clssig.data() + clssig.size());

							for (auto& arg : arglist)
							{
								arg.first = oldsig;
								arg.second = oldsig = ParseSignature(oldsig);

								if (args[2] == L"PROXY")
									sig.insert(sig.end(), arg.first, arg.second);
							}

							if (convention & IMAGE_CEE_CS_CALLCONV_HASTHIS)
								arglist.insert(arglist.begin(), std::make_pair(clssig.data(), clssig.data() + clssig.size()));

							PCCOR_SIGNATURE locsig;
							ULONG loclen;
							if ((p[0] & 7) == 3  && *reinterpret_cast<uint32_t*>(&il[8]))
							{
								hr = import->GetSigFromToken(*reinterpret_cast<uint32_t*>(&il[8]), &locsig, &loclen);
								if (FAILED(hr))
									throw L"Failed GetSigFromToken for " + std::to_wstring(*reinterpret_cast<uint32_t*>(&il[8]));
							}
							else
								locsig = reinterpret_cast<const unsigned char*>("\0\0");
							std::vector<std::pair<PCCOR_SIGNATURE, PCCOR_SIGNATURE>> loclist(CorSigUncompressData(++locsig));
							for (auto& loc : loclist)
							{
								loc.first = locsig;
								loc.second = locsig = ParseSignature(locsig);
							}

							if (args[2] == L"AFTER_NORET" || args[2] == L"AFTER_RET")
							{
								for (size_t idx = 0; idx < arglist.size(); ++idx)
								{
									if (idx >= 4)
										il.push_back(0x0E); // ldarg.s
									il.push_back((idx >= 4 ? 0x00 : 0x02) + static_cast<uint8_t>(idx)); // idx or ldarg.idx
								}

								// method call
								il.push_back(0x28);
								il.push_back((method >> 0) & 0xFF);
								il.push_back((method >> 8) & 0xFF);
								il.push_back((method >> 16) & 0xFF);
								il.push_back((method >> 24) & 0xFF);
							}

							if (args[2] == L"AFTER_RET" && returnsig != ELEMENT_TYPE_VOID)
							{
								if (argList.size() > 0 && argList[0] == L"r")
									;
								else
									il.push_back(0x26); // pop
							}

							for (const auto& arg : argList)
							{
								wchar_t* p = const_cast<wchar_t*>(arg.c_str());

								bool isgeneric = *p == L'<';
								if (isgeneric)
									++p;

								switch (*p++)
								{
								case L'a':
								{
									const auto idx = std::wcstoul(p, &p, 10);
									if (idx >= arglist.size())
										throw L"Index exceeds arg count (" + std::to_wstring(idx) + L" / " + std::to_wstring(arglist.size()) + L")";

									if (*p == L'<')
									{
										if (*++p != L'>')
											throw L"Unknown argument identifier: " + arg;
									}

									if (*p == L'&')
										il.push_back(0x0F); // ldarga.s
									else
										if (idx >= 4)
											il.push_back(0x0E); // ldarg.s
									il.push_back((*p == L'&' || idx >= 4 ? 0x00 : 0x02) + static_cast<uint8_t>(idx)); // idx or ldarg.idx

									if (*p != L'.')
									{
										if (*p == L'&')
											sig.push_back(ELEMENT_TYPE_BYREF);
										if (!isgeneric)
											sig.insert(sig.end(), arglist[idx].first, arglist[idx].second);
										else
										{
											sig.push_back(ELEMENT_TYPE_MVAR);
											sig.resize(sig.size() + 4);
											sig.resize(sig.size() - 4 + CorSigCompressData(genericidx++, &sig[sig.size() - 4]));
											inst.insert(inst.end(), arglist[idx].first, arglist[idx].second);
										}
									}
									break;
								}
								case L's':
								{
									const auto token = std::wcstoul(p, &p, 16);

									il.push_back(*p == '&' ? 0x7F : 0x7E); // ldsflda or ldsfld
									il.push_back((token >> 0) & 0xFF);
									il.push_back((token >> 8) & 0xFF);
									il.push_back((token >> 16) & 0xFF);
									il.push_back((token >> 24) & 0xFF);

									if (*p != L'.')
									{
										PCCOR_SIGNATURE fldsig;
										auto hr = import->GetFieldProps(token, nullptr, nullptr, 0, nullptr, nullptr, &fldsig, nullptr, nullptr, nullptr, nullptr);
										if (FAILED(hr))
											throw L"No field " + std::to_wstring(token) + L" defined";
										if (*p == '&')
											sig.push_back(ELEMENT_TYPE_BYREF);
										if (!isgeneric)
											sig.insert(sig.end(), fldsig + 1, ParseSignature(fldsig + 1));
										else
										{
											sig.push_back(ELEMENT_TYPE_MVAR);
											sig.resize(sig.size() + 4);
											sig.resize(sig.size() - 4 + CorSigCompressData(genericidx++, &sig[sig.size() - 4]));
											inst.insert(inst.end(), fldsig + 1, ParseSignature(fldsig + 1));
										}
									}
									break;
								}
								case L'l':
								{
									const auto idx = std::wcstoul(p, &p, 10);
									if (idx >= loclist.size())
										throw L"Index exceeds local count (" + std::to_wstring(idx) + L" / " + std::to_wstring(loclist.size()) + L")";

									if (*p == L'&')
										il.push_back(0x12); // ldloca.s
									else
										if (idx >= 4)
											il.push_back(0x11); // ldloc.s
									il.push_back((*p == L'&' || idx >= 4 ? 0x00 : 0x06) + static_cast<uint8_t>(idx)); // idx or ldloc.idx

									if (*p != L'.')
									{
										if (*p == L'&')
											sig.push_back(ELEMENT_TYPE_BYREF);
										if (!isgeneric)
											sig.insert(sig.end(), loclist[idx].first, loclist[idx].second);
										else
										{
											sig.push_back(ELEMENT_TYPE_MVAR);
											sig.resize(sig.size() + 4);
											sig.resize(sig.size() - 4 + CorSigCompressData(genericidx++, &sig[sig.size() - 4]));
											inst.insert(inst.end(), loclist[idx].first, loclist[idx].second);
										}
									}
									break;
								}
								case L'r':
									continue;
								default:
									throw L"Unknown argument identifier: " + arg;
									continue;
								}

								while (*p == L'.')
								{
									const auto type = *++p;
									const auto token = std::wcstoul(++p, &p, 16);

									switch (type)
									{
									case 'f':
										il.push_back(*p == '&' ? 0x7C : 0x7B); // ldflda or ldfld
										break;
									case 'v':
									case 'm':
										il.push_back(0x6F); // callvirt
										break;
									default:
										throw L"Unknown argument identifier: " + arg;
										continue;
									}

									il.push_back((token >> 0) & 0xFF);
									il.push_back((token >> 8) & 0xFF);
									il.push_back((token >> 16) & 0xFF);
									il.push_back((token >> 24) & 0xFF);

									if (*p != L'.')
									{
										PCCOR_SIGNATURE fldsig;
										switch (type)
										{
										case 'f':
										{
											auto hr = import->GetFieldProps(token, nullptr, nullptr, 0, nullptr, nullptr, &fldsig, nullptr, nullptr, nullptr, nullptr);
											if (FAILED(hr))
												throw L"No field " + std::to_wstring(token) + L" defined";
											if (*p == '&')
												sig.push_back(ELEMENT_TYPE_BYREF);
											++fldsig;
											break;
										}
										case 'v':
										{
											auto hr = import->GetMethodProps(token, nullptr, nullptr, 0, nullptr, nullptr, &fldsig, nullptr, nullptr, nullptr);
											if (FAILED(hr))
												throw L"No method " + std::to_wstring(token) + L" defined";
											if (CorSigUncompressData(++fldsig) != 0)
												throw L"Number of args not match for method " + std::to_wstring(token);
											break;
										}
										case 'm':
										{
											auto hr = import->GetMemberRefProps(token, nullptr, nullptr, 0, nullptr, &fldsig, nullptr);
											if (FAILED(hr))
												throw L"No method " + std::to_wstring(token) + L" defined";
											if (CorSigUncompressData(++fldsig) != 0)
												throw L"Number of args not match for method " + std::to_wstring(token);
											break;
										}
										default:
											continue;
										}
										if (!isgeneric)
											sig.insert(sig.end(), fldsig, ParseSignature(fldsig));
										else
										{
											sig.push_back(ELEMENT_TYPE_MVAR);
											sig.resize(sig.size() + 4);
											sig.resize(sig.size() - 4 + CorSigCompressData(genericidx++, &sig[sig.size() - 4]));
											inst.insert(inst.end(), fldsig, ParseSignature(fldsig));
										}
									}
								}
							}

							mdMemberRef memberRef = mdMemberRefNil;
							hr = metaDataEmit->DefineMemberRef(typeRef, methodName.c_str(), sig.data(), sig.size(), &memberRef);
							if (FAILED(hr))
								throw L"Failed DefineMemberRef " + methodName + L" for " + std::to_wstring(typeRef);

							if (generics)
							{
								hr = metaDataEmit->DefineMethodSpec(memberRef, inst.data(), inst.size(), &memberRef);
								if (FAILED(hr))
									throw L"Failed DefineMethodSpec " + methodName + L" for " + std::to_wstring(typeRef);
							}

							// method call
							if (args[2] != L"PROXY")
								il.push_back(0x28);
							il.push_back((memberRef >> 0) & 0xFF);
							il.push_back((memberRef >> 8) & 0xFF);
							il.push_back((memberRef >> 16) & 0xFF);
							il.push_back((memberRef >> 24) & 0xFF);

							if (args[2] == L"BEFORE")
							{
								for (size_t idx = 0; idx < arglist.size(); ++idx)
								{
									if (idx >= 4)
										il.push_back(0x0E); // ldarg.s
									il.push_back((idx >= 4 ? 0x00 : 0x02) + static_cast<uint8_t>(idx)); // idx or ldarg.idx
								}

								// method call
								il.push_back(0x28);
								il.push_back((method >> 0) & 0xFF);
								il.push_back((method >> 8) & 0xFF);
								il.push_back((method >> 16) & 0xFF);
								il.push_back((method >> 24) & 0xFF);
							}

							if (args[2] == L"INJECT" || args[2] == L"PROXY")
							{
								if (static_cast<int>(il.size()) > (((p[0] & 7) == 3 ? 12 : 1) + injectpos.second))
									throw L"Injected code is larger than injection range (" + std::to_wstring(injectpos.second - injectpos.first) + L" / " + std::to_wstring(il.size() - ((p[0] & 7) == 3 ? 12 : 1) - injectpos.first) + L")";

								il.insert(il.end(), (((p[0] & 7) == 3 ? 12 : 1) + injectpos.second) - il.size(), 0x00); // nop
								il.insert(il.end(), p + ((p[0] & 7) == 3 ? 12 : 1) + injectpos.second, p + s);
							} else
								// ret
								il.push_back(0x2a);

							if (args[2] != L"PROXY")
								if ((il[0] & 7) == 3)
								{
									if (args[2] != L"INJECT")
									{
										*reinterpret_cast<uint16_t*>(&il[2]) = static_cast<uint16_t>(std::max(argList.size() + 1, args[2] == L"REPLACE" ? 0 : arglist.size()));
										*reinterpret_cast<uint16_t*>(&il[0]) &= ~0x0008;
										*reinterpret_cast<uint32_t*>(&il[4]) = il.size() - 12;
									} else
										*reinterpret_cast<uint16_t*>(&il[2]) += static_cast<uint16_t>(argList.size());
								}
								else
								{
									il[0] &= 3;
									il[0] |= (il.size() - 1) << 2;
								}

							auto allocated = static_cast<uint8_t*>(methodMalloc->Alloc(il.size()));
							std::copy(il.begin(), il.end(), allocated);
							hr = info->SetILFunctionBody(moduleID, token, allocated);
							if (FAILED(hr))
								throw L"Failed SetILFunctionBody for " + std::to_wstring(token);
						} else
						if (args[0] == L"Patch")
						{
							if (args.size() < 3)
								throw std::wstring(L"Too few arguments for type Patch");

							const auto token = std::wcstoul(args[1].c_str(), nullptr, 16);
							const auto offset = std::wcstoul(args[2].c_str(), nullptr, 16);

							LPCBYTE p;
							ULONG s;
							auto hr = info->GetILFunctionBody(moduleID, token, &p, &s);
							if (FAILED(hr))
								throw L"Failed GetILFunctionBody for " + std::to_wstring(token);

							auto allocated = static_cast<uint8_t*>(methodMalloc->Alloc(s));
							std::copy(p, p + s, allocated);
							for (size_t i = 0; i < args.size() - 3; ++i)
							{
								const auto value = std::wcstoul(args[i + 3].c_str(), nullptr, 16);
								allocated[offset + i + ((p[0] & 7) == 3 ? 12 : 1)] = static_cast<uint8_t>(value);
							}

							hr = info->SetILFunctionBody(moduleID, token, allocated);
							if (FAILED(hr))
								throw L"Failed SetILFunctionBody for " + std::to_wstring(token);
						}
						else
							throw L"Unknown type " + args[0];
					}
				}
				catch (const std::wstring& e)
				{
					MessageBox(nullptr,
						(L"Error: " + e + L"\nat " + filename + L":" + std::to_wstring(lineno) + L"\n" + line).c_str(),
						L"profiler.dll", MB_OK);
				}
			}

			if (!versions.count(version))
			{
				auto message = L"Warning: " + asmname + L" does not support your version " + version + L".\n  Candidates are:";
				for (const auto& v : versions)
					message += L"\n  - " + v;
				MessageBox(nullptr,
					message.c_str(),
					L"profiler.dll", MB_OK);
			}
		} while (FindNextFile(hFind, &win32fd));

		FindClose(hFind);
	}
	
	methodMalloc->Release();
	metaDataAssemblyImport->Release();
	metaDataAssemblyEmit->Release();
	metaDataEmit->Release();
	import->Release();

	return S_OK;
}
catch (const std::wstring& e)
{
	MessageBox(nullptr,
		(L"Error: " + e).c_str(),
		L"profiler.dll", MB_OK);

	return S_OK;
}
