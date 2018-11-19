#include <iostream>

#include <string>

#include <Windows.h>

int WINAPI WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPSTR lpCmdLine, int nCmdShow)
{
	STARTUPINFO si = { sizeof(STARTUPINFO) };
	PROCESS_INFORMATION pi = {};
	char path[MAX_PATH + 1];
	GetCurrentDirectory(MAX_PATH + 1, path);

	std::string target(lpCmdLine[0] ? lpCmdLine : std::string(path) + "\\BveTs.exe");
	std::string file(target);
	file[file.rfind('\\')] = '\0';

	SetEnvironmentVariable("COR_ENABLE_PROFILING", "1");
	SetEnvironmentVariable("COR_PROFILER", "{7ADA6F81-2F62-4432-8BA0-C18CECAE1546}");
	SetEnvironmentVariable("COR_PROFILER_PATH", (std::string(path) + "\\profiler.dll").c_str());
	SetEnvironmentVariable("COMPLUS_Version", "v4.0.30319");
	auto env = GetEnvironmentStrings();

	CreateProcess(
		target.c_str(),
		nullptr,
		nullptr,
		nullptr,
		false,
		0,
		env,
		file.c_str(),
		&si,
		&pi);

	FreeEnvironmentStrings(env);

	MSG msg = {};
	TranslateMessage(&msg);

	CloseHandle(pi.hThread);

	WaitForSingleObject(pi.hProcess, INFINITE);
	
	return 0;
}
