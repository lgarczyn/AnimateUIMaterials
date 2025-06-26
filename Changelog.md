
# 1.3.0
* Support custom shader property drawers
* Hide UI shader useless property names
* Added new override script for rare occasions when texture scale and offset are overriden
* Fixed mask issues

# 1.2.0
* Massively reduces number of materials created

# 1.1.7
* Fixed Vector property drawer

# 1.1.6
* Fixed build
* Added context menu option to reload from modified source material
* Made it impossible to accidentally modify the generated material in editor

# 1.1.5
* Added support for HDR colors
* Fixed slider not appearing in very specific circumstances

# 1.1.4
* Fixed warnings on adding some material properties
* Fixed alignement issues in GraphicMaterialOverride table

# 1.1.3
* Added multi-edit to properties
* Better checks for detecting animated changes
* Fixed modified material staying when removing the material override in editor
* Changed object field to button to select the property object
* Upgraded project

# 1.1.2
* Fixed build on 2022
* Fixed Editor reference in build

# 1.1.1
* Fixed editor alignement using vertical scopes instead
* Added hamburger menu

# 1.1.0
* Added animation system callbacks for efficiency
* Fixed material updating too often
* Fixed readme

# 1.0.0
* Added master material override component
* Added editor to visualize modified properties from the GraphicMaterialOverride component
* Added property overrides for each property type
* Added editor to filter out compatible properties