﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.Extensions.DependencyModel;



#if NETCOREAPP3_0_OR_GREATER
using System.IO;
using System.Reflection;
using Natasha.CSharp.Component.Domain;
/// <summary>
/// 程序集编译构建器 - 编译选项
/// </summary>
public sealed partial class AssemblyCSharpBuilder
{
    
    private Func<IEnumerable<MetadataReference>, IEnumerable<MetadataReference>>? _referencesFilter;
    private CombineReferenceBehavior _combineReferenceBehavior = CombineReferenceBehavior.UseCurrent;
    private readonly ReferenceConfiguration _referenceConfiguration = new();


    /// <summary>
    /// 编译时，使用主域引用覆盖引用集,并配置同名引用版本行为(默认优先使用主域引用)
    /// </summary>
    /// <param name="action">配置委托</param>
    /// <returns></returns>
    public AssemblyCSharpBuilder WithCombineReferences(Action<ReferenceConfiguration>? action = null)
    {
        action?.Invoke(_referenceConfiguration);
        _combineReferenceBehavior = CombineReferenceBehavior.CombineDefault;
        return this;
    }

    /// <summary>
    /// 编译时，使用当前域引用来覆盖引用集
    /// </summary>
    /// <returns></returns>
    public AssemblyCSharpBuilder WithCurrentReferences()
    {
        _combineReferenceBehavior = CombineReferenceBehavior.UseCurrent;
        return this;
    }

    private readonly List<MetadataReference> _specifiedReferences;
    /// <summary>
    /// 使用外部指定的引用来覆盖引用集
    /// </summary>
    /// <param name="metadataReferences"></param>
    /// <returns></returns>
    public AssemblyCSharpBuilder WithSpecifiedReferences(IEnumerable<MetadataReference> metadataReferences)
    {
        lock (_specifiedReferences)
        {
            _specifiedReferences.AddRange(metadataReferences);
        }
        _combineReferenceBehavior = CombineReferenceBehavior.UseSpecified;
        return this;
    }
    public AssemblyCSharpBuilder ClearOutsideReferences()
    {
        lock (_specifiedReferences)
        {
            _specifiedReferences.Clear();
        }
        return this;
    }

    private readonly List<MetadataReference> _dependencyReferences;
    /// <summary>
    /// 设置依赖元数据引用，依赖元数据总是会被加载
    /// </summary>
    /// <param name="metadataReferences"></param>
    /// <returns></returns>
    public AssemblyCSharpBuilder WithDependencyReferences(IEnumerable<MetadataReference> metadataReferences)
    {
        lock (_dependencyReferences)
        {
            _dependencyReferences.AddRange(metadataReferences);
        }
        return this;
    }
    public AssemblyCSharpBuilder ClearDependencyReferences()
    {
        lock (_dependencyReferences)
        {
            _dependencyReferences.Clear();
        }
        return this;
    }

    /// <summary>
    /// 配置引用过滤策略
    /// </summary>
    /// <param name="referencesFilter"></param>
    /// <returns></returns>
    public AssemblyCSharpBuilder SetReferencesFilter(Func<IEnumerable<MetadataReference>, IEnumerable<MetadataReference>>? referencesFilter)
    {
        _referencesFilter = referencesFilter;
        return this;
    }

    /// <summary>
    /// 流编译成功之后触发的事件
    /// </summary>
    public event Action<CSharpCompilation, Assembly>? CompileSucceedEvent;



    /// <summary>
    /// 流编译失败之后触发的事件
    /// </summary>
    public event Action<CSharpCompilation, ImmutableArray<Diagnostic>>? CompileFailedEvent;


    public CSharpCompilation GetAvailableCompilation(Func<CSharpCompilationOptions, CSharpCompilationOptions>? initOptionsFunc = null)
    {
#if DEBUG
        Stopwatch stopwatch = new();
        stopwatch.Start();
#endif

        //Mark : 26ms
        //if (_compileReferenceBehavior == PluginLoadBehavior.None)
        //{
        //    _compilerOptions.WithLowerVersionsAssembly();
        //}

        var options = _compilerOptions.GetCompilationOptions(_codeOptimizationLevel,_withDebugInfo);
        if (initOptionsFunc != null)
        {
            options = initOptionsFunc(options);
        }
        IEnumerable<MetadataReference> references;
        if (_combineReferenceBehavior == CombineReferenceBehavior.CombineDefault)
        {
            references = Domain.GetReferences(_referenceConfiguration);
        }
        else if(_combineReferenceBehavior == CombineReferenceBehavior.UseCurrent)
        {
            references = Domain.References.GetReferences();
        }
        else
        {
            references = _specifiedReferences;
        }

        if (_referencesFilter != null)
        {
            references = _referencesFilter(references);
        }

        if (_dependencyReferences.Count > 0)
        {
            references = references.Concat(_dependencyReferences);
        }

        _compilation = CSharpCompilation.Create(AssemblyName, SyntaxTrees, references, options);
#if DEBUG
        stopwatch.StopAndShowCategoreInfo("[Compiler]", "获取编译单元", 2);
#endif

        if (EnableSemanticHandler)
        {
            foreach (var item in _semanticAnalysistor)
            {
                _compilation = item(this, _compilation, _semanticCheckIgnoreAccessibility);
            }
        }
        return _compilation;
    }


}
#endif

