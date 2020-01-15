#include <iostream>

extern "C" __declspec(dllexport) int __stdcall CountLettersInString(wchar_t* str)
{
    int length = 0;
    while (*str++ != L'\0')
        length++;
    return length;
}

extern "C" __declspec(dllexport) void __stdcall LogThis(wchar_t* str)
{
    std::cout << str << "\n";
}