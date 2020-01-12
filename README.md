# ElasticSearch7Template 
 该项目为ElasticSearch的webapi项目， 基于.NET CORE 3.1 和NEST  7.2.1，低于7.X的，请自行改写DAL层即可，
 项目面向接口编程，鉴于es数据逻辑一般不会太复杂，简化了整个项目接口，项目中使用Scrutor自动扫描注入，
 
##  项目结构： ##
ElasticSearch7Template
   

- .template.config  （.NET CORE 脚手架模板）
- ElasticSearch7Template （api）
- ElasticSearch7Template.BLL (业务逻辑)
- ElasticSearch7Template.BLL.MediatR （业务逻辑中介通用层基于MediatR，此处暂时没用到）
- ElasticSearch7Template.Core（公共核心类）
- ElasticSearch7Template.DAL （仓储层)
- ElasticSearch7Template.Entity(实体)
- ElasticSearch7Template.IBLL （业务接口）
- ElasticSearch7Template.IDAL （仓储接口）
- ElasticSearch7Template.Model （model层）
- ElasticSearch7Template.Utility （工具类）

## 使用 ##

 项目约定：
 方便理解整个项目的情况，项目中基于一些约定
  
1. 类的命名，api的实际编写其实基于表结构自动生成，如表名demo
	1. ibll接口为 
		1. IQueryDemoService 包含增删改
		2. ICommandDemoService,只包含包含查询
	2. bll接口实现为 （统一后缀Impl）
		1. QueryDemoServiceImpl 包含增删改
		2. CommandDemoServiceImpl,只包含包含查询
	3. IDAL接口为：
		1. IDemoRepository
	4. DAL接口实现为:
		1. DemoRepository
	5. Model对应的查询条件类为：
		1. BaseDemoCondition
	6. Entity对应的类：
		1. DemoEntity
		2. PartOfDemoEntity 
	7. controller对应的类为：
		1. DemoController  es数据接口controller
		2. DemoIndexController es索引文件接口controller
		3. DemoTemplateController es模板文件接口controller
2. 索引对应的默认名称，别名，模板名称，统一在实体的特性上设置如下：
[ElasticsearchIndex(DefaultIndexName = "demo", Alias = "demo_alias", TemplateName = "demo_template")]
2. [ElasticsearchType(RelationName = "_doc", IdProperty = "id")]
3. 项目根据模块分组，api同时也根据模板分组，不分组可能会出现api swagger不展示
4. 依赖注入：项目中使用Scrutor自动扫描注入（详情见Core项目IAutoInject，api项目下RegisterService文件），以下为瞄点
	1. IAutoInject，IScopedAutoInject   自动注入扫描点默认为Scoped 
	3. ISingletonAutoInject 自动注入接口和实现Singleton类型
	4. ITransientAutoInject 自动注入接口和实现Transient类型
	5. ISelfScopedAutoInject 自动注入自身Scoped类型
	6. ISelfSingletonAutoInject 自动注入自身Singleton类型
	7. ISelfTransientAutoInject 自动注入自身Transient类型


## 下载安装 ##
项目配备了dotnet cli的模板功能，可以直接安装进行替换，省去修改命名空间等繁琐操作，详情了解.template.config 文件，
 

1. 下载后，进入目录，和（.template.config同级）


1. 运行 dotnet new -i . (-i 后面有个 点“.”) 安装模板


1. 安装成功后运行dotnet new，可看见安装的模板 myesapi 
2. 进入项目目录 dotnet new myesapi XXX，创建新的es api项目
	1.  XXX 为你的项目名称，如  dotnet new myesapi test,会创建一个项目目录test，内部包含，整个项目的源码
	2. ElasticSearch7Template  会被全局替换为test，包括文件夹的名称
3. dotnet build 或者dotnet run 即可

## 说明 ##

1. api 只是初级版，针对es中的复杂场景，请自行拓展。
2. 项目提供了基于sql的查询，请自行根据es的语法编写，针对Object，Nested 类型没有提供支持。
2. 出于各种原因考虑，生成器暂时不开源，代码可能存在bug，请提交pr，thanks，此外不提供技术支持。

## 演示地址 ##




    
 
