


typedef void* MonoDomain;
typedef void* MonoAssembly;
typedef void* MonoImage;
typedef void* MonoClass;
typedef void* MonoObject;
typedef void* MonoMethodDesc;
typedef void* MonoMethod;
typedef int gboolean;


extern "C" {
    
    MonoDomain *mono_domain_get();
    
    MonoAssembly *mono_domain_assembly_open(MonoDomain *domain, const char *assemblyName);
    
    MonoImage *mono_assembly_get_image(MonoAssembly *assembly);
    
    MonoMethodDesc *mono_method_desc_new(const char *methodString, gboolean useNamespace);
    
    MonoMethodDesc *mono_method_desc_free(MonoMethodDesc *desc);
    
    MonoMethod *mono_method_desc_search_in_image(MonoMethodDesc *methodDesc, MonoImage *image);
    
    MonoObject *mono_runtime_invoke(MonoMethod *method, void *obj, void **params, MonoObject **exc);
    
    MonoObject *mono_gchandle_get_target(void* obj);
    
}
