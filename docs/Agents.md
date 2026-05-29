# TplQueue.Adapter End user documentation

## Context

I am writting the documeantation of the software repository Fmacias.TplQueue.Adaper which url is 
https://github.com/fmacias/TplQueue.Adapter. 

The documenation to be extended unther is unther https://github.com/fmacias/TplQueue.Adapter/tree/main/docs and it is composed 
in such way that can easyly be exported to fmacias.github.io, a mkdocs site due to a simple copy on deploying, this is the reason
because the structure is built in such static web site style way. 

I like the way the microsoft documenation is written, for example this site `https://learn.microsoft.com/en-us/aspnet/core/data/ef-rp/intro?view=aspnetcore-10.0&tabs=visual-studio`
More like usage with its explanation starting from the installation  or from Nuget package binding in my case.

## TplQueue.Adaper repository

The actual kaotical and "all in one file" documentation in this direcction is found into the `https://github.com/fmacias/TplQueue.Adapter/blob/main/docs/reference.md`
site, it commes from a previous development time, when I packed all into one file, into the readme file. Now the readme file is a part of the binary
and ist mostly only operational. The rest of the documenation is localized outside the readme, in a normal way, and because of this
the "old" readme file is now the "reference.md" file.

Check the readme file for your understanding and rescue some paragraph of secction if required.

### Repository content and Fmacias.TplQueue architecture.

Details about it are found curreently  in reference.md.

In few words, is the fasade of ICoreApi plus few defined Observers, Retry policies, cache implementation fundamental and default MemCache implemenation for test scenarios,
and seriazation component. Thos related components are considered subcomponent of ICoreApi. This is the reason beacuse their factories are public and the only facade
is the API of Fmacias.TplQueue.

The component Fmacias.TplQueue.Microsoft.DependencyInjection is the one to be used in the examples, because is the one to get integrated into one ASP.NET application, for example.
Because of this, the tutorial should explain how to integrate this component into one ASP.Net Application.



## Repository documentation

When modifying documentation for this repository:

1. Treat the root `README.md` as the concise repository and package entry point and part of binary. It is not considered part of the documenation.
2. Consider the content of the `docs/en/` and `docs/de/` folders as the publishable tutorial trees of this repository.
3. Treat `docs/en/` and `docs/de/` as the long-form repository documentation trees.
   - Keep content grouped by the current sections:
     - `Getting Started`: 
		* Sumarized MD file with the title "TplQueue with .NET and ASP.NET"
		* First paragraph with the explanation, links to the mentioned subcompnents or related repositories like TplQueue.Usage(https://github.com/fmacias/TplQueue.Usage)
		to the personal site (htpps://fmacias.github.io), and to TplQueue.Core access requestor site in (https://fmacias.github.io/tplqueue/license/core-license/) as well.
		* Prerequisites: About the Nuget packages to install, in this case `Fmacias.TplQueue.Microsoft.DependencyInjection` and `Fmacias.TplQueue.Core`.
		Add a note to highlite that the core component can be used gratis, but request for a lincence give you a profesional service support to improve your leggacy solution.
		* Load Configuration: Note that the DI component has teh avility to load the retry policies and queues from predefined application configuration.
			- Explain that Queues and be instantiated determistically, with an ID if neccessary to be loaded from external systems.
		* Instance a ParallelQ queue from configuration. (name that there are serveral overload to instantiate queue, and add a link to https://github.com/fmacias/TplQueue.Adapter/blob/main/src/Fmacias.TplQueue/Factories/QFactoryAdapter.cs and https://github.com/fmacias/TplQueue.Adapter/blob/main/src/Fmacias.TplQueue/API.cs
			- Add a job graph: Extract->Transfor->Load scenario from TplQueue.Usage smoke test programs.
			- Add a Closure Async task job: Also from TplQueue.Usage component.
		* Instance a FifoQ queue from configuration. (name that there are serveral overload to instantiate queue, and add a link to https://github.com/fmacias/TplQueue.Adapter/blob/main/src/Fmacias.TplQueue/Factories/QFactoryAdapter.cs and https://github.com/fmacias/TplQueue.Adapter/blob/main/src/Fmacias.TplQueue/API.cs
			- Create the following sequence in for steps to be executed sequetoially in async closures that ensure that all will be treated sequentially allthought the methods are async.
			- Extract->TransForm->Load->AfterLoad in four closure steps, no jobs.
		* Instance a CacheQ using a dedicated parallel queue loaded from configuration.
			- Add a job graph: Extract->Transfor->Load scenario from TplQueue.Usage smoke test programs where each transformed payload object 
			should be saved in file as JSON system. 
	 - `architecture`: like it is for now
     - `development`: like it is for now
     - `operations`: like it is for now
     - `reference`: Remove content fi recicle on creating the final documenation.

4. Keep terminology aligned with the current public model.
   - Use `IJob`, `IJobRoot`, `IDataJob`, `IDataJobRoot`, `IQ`, `IParallelQ`, `IFifoQ`, and `ICacheQ`.
   - Do not reintroduce legacy `TaskRunner` terminology unless it is explicitly called out as historical context.

5. Keep runnable examples centralized in `TplQueue.Usage`.
   - Core docs may include small focused snippets.
   - Full consumer-facing sample flows should link to `TplQueue.Usage` instead of being duplicated here.

6. Keep `TplQueue.Adapter/docs/en/` and `docs/de/` as the only public documentation source-of-truth trees imported by `fmacias.github.io`.
   - Public links to repositories such as `TplQueue.Abstractions` and `TplQueue.Usage` are allowed.
   - Small focused code blocks derived from those public repositories are allowed when they improve the documentation.
   - Do not turn the site publication model into a multi-repository docs import workflow.

7. Keep documentation tightly coupled to the owning surface.
   - `docs/en/` and `docs/de/`: repository-level explanations and grouped reference material.
   - Diagrams: publish rendered SVG artifacts under `docs/en/architecture/rendered/` and `docs/de/architecture/rendered/` when they are intended for the public site.
   - Keep PlantUML source files private in `TplQueue.Core` unless the human explicitly asks to publish them.

8. Keep the Core access policy explicit.
   - Official NuGet packages are publicly consumable under the package-specific license.

## Cross-repository documentation alignment

When a documentation task also affects another repository, the public site, publishing, navigation, or license wording:

1. Read the root `Agents.md` or `AGENTS.md` and the root `README.md` of every directly affected repository before editing.
2. Read this file after the root files and treat it as the detailed instruction set for the `docs/` trees.
3. Compare the requested change against the current documented source-of-truth model and identify contradictions before broad rewrites.
4. Do not silently normalize contradictions across repositories.
5. If the human instruction conflicts with the existing documentation or instruction files and intent is not already explicit, ask whether to:
   - align the documentation to the current human instruction and treat the inconsistency as an exception, or
   - preserve the existing inconsistency as the current operating rule.
6. When proceeding, update the relevant `README.md`, `Agents.md` or `AGENTS.md`, and sync or publishing instructions together so the documentation boundary remains aligned.
   - The source repository remains private.
   - Source access, support, maintenance, and rights around modified-source distribution require a separate written agreement and explicit approval.

---


### Documenation Structure

 Treat `docs/en/` and `docs/de/` as the long-form repository documentation trees.
   - Keep content grouped by the current sections:
     - `Getting Started`: 
     - `architecture`
     - `development`
     - `operations`
     - `reference`
