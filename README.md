# PlasmaCustomAgents
A library to unify and streamline the process of adding custom nodes and components into Plasma

### Create a custom node

1. Create a new class that inherits `PlasmaCustomAgents.CustomAgent`
2. Create a gestalt using `CustomAgentManager.CreateNodeGestalt`
3. Add any ports to the gestalt using `CustomAgentManager.CreateCommandPort`, `CustomAgentManager.CreatePropertyPort` and `CustomAgentManager.CreateOutputPort`
4. Add gestalt to game's gestalt list with `CustomAgentManager.RegisterGestalt`

### Create a custom component

1. Create a gestalt using `CustomAgentManager.CreateComponentGestalt`
2. Change gestalt's properties, such as componentScale*Limits, 
3. Add gestalt to game's gestalt list with `CustomAgentManager.RegisterGestalt`

### Getting a prefab to use in `CustomAgentManager.CreateComponentGestalt`

Here will be link to the guide on setting up a unity project for creating component prefabs

# TODO
* AssetBundle/Unity project guide
* More testing and documentation on interactive components (e.g. buttons and such)

# Releases

### 1.0.1
* Update readme

### 1.0.0
* Initial release