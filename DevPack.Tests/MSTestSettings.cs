#if CI
[assembly: Parallelize(Workers = 4, Scope = ExecutionScope.MethodLevel)]
#else
[assembly: Parallelize(Scope = ExecutionScope.MethodLevel)]
#endif