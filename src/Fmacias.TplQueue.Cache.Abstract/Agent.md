# Context

## Sumary

First, work always in Thinking mode. Not instante mode.

You are working on the TplQueue.Adapter repository, which folder is accessible in this session.
The subfolders of the .Net projects are:
- src: source code of the project.
- test:  
Unit test and integration test of the projects.

Now you have to concentrate yourself at the projects Fmacias.TplQueue.Cache.Abstract, Fmacias.TplQueue.Cache.MemCache,
Fmacias.TplQueue and Fmacias.TplQueue.Microsoft.DependencyInjection.

## Roles
- to refactor and reviewing act as a Software Enginer.
- for documentation purpouses, act as a Software Architekt. 

## Review, refactor and reimplementation rules

- SOLID (SRP, OCP, LSP, ISP, DIP)
- DRY, KISS, YAGNI
- Separation of Concerns
- Fail Fast, defensive programming
- Immutability by default where reasonable
- Clean, readable code, no unnecessary complexity
- NUnit tests using Arrange/Act/Assert, with Moq where appropriate
- DI 
- no singletons 
- “no new fuera del composition root”

### Code Style:
# Instructions

0. I have changed the class Fmacias.TplQueue.API and now the tests, test's Helpers, dependecy injection project and any related dependent service should corrected correspondingly.

1. Analyze all public and internal services of all services mentioned at least in the context section.
   - Identify duplication, long methods, missing null checks, unexpected side effects.Defensive programing.

2. Apply refactors safelly:
   - Fix obviously incorrect logic.
   - Add guard clauses and argument validation (throwing appropriate exceptions).
   - Extract small private methods from oversized methods where it clearly improves readability. Apply SRP to private logic, to read the method in procedural style like prosa.
   - Remove dead private fields or methods that are never used.
   - Improve XML documentation comments where missing or misleading in profesional English.

3. Very important: Correct the test so that they resolve the current version of  Fmacias.TplQueue.API:
   - Cover edge cases: zero/negative values, null arguments, invalid states.
   - Cover error paths: exceptions thrown when preconditions are violated.
   - Ensure that each key service has at least one focused test class.
   - Use NUnit and Moq following AAA (Arrange, Act, Assert).
   - Refactor the test project, and take care that you do not remove any existing one, for better readability
5. Documenation:
	- Enhance the readme file of changed modules.
	- Shold have a a sumary, table of context with a link to each created section.
	- The readme file requires at least a seccion with the sumary, how to implementation of each involved module, 
	with one step by step implementation example about the "how to" use. Usage mainly.
	- Justification's design about decition of the implementation, for example the factory pattern and static create method of the internal DomainObject, 
	name the adavantages of this desing decision. Identify other desing patterns used naturally at the module and explain them also in the same way.
	
4. Check the packaging powershell files. Notice that the project `WorkspaceTplQueue` converts at implementation local packages dependencies to project dependencies for ergonmic experience. 
The project Fmacias.TplQueue.Cache.MemCache and Fmacias.TplQueue.Cache.MemCache.Test should also be added to this bussiness logic.

5. Before running the tests or let me know to make a human check to the changes  and to try to run the tests with project dependencies. Before building the packanges.
 
4. Run the tests, anlyse the output and correct them accordingly:
   - dotnet test TplQueue.Cache.Abstract.Text, if the command is not right, correct it and use the right one.

CONSTRAINTS

- Do NOT change namespaces or public API signatures unless strictly necessary to fix a bug.
- Do NOT introduce new external dependencies.
- Keep changes small, understandable, and consistent with the current style.
- Apply all changes directly to the workspace without asking for confirmation.
