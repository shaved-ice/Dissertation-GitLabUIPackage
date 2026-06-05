# Dissertation-GitLabUIPackage
An upload of my completed dissertation from my undergraduate degree.\
The end result was a Unity Package for abstracting the development of games with a GitLab UI. More specifically it abstracts the creation, receiving, and sending of web requests from the game to GitLab.\
This is a upload of the most important files from my Dissertation's GitLab repository - less important files such as log books or report drafts have not been uploaded in order to make browsing the repository easier. 

### Final Report
The final dissertation report PDF can be found [here](https://github.com/shaved-ice/Dissertation-GitLabUIPackage/blob/main/FinalDissertationReport.pdf). Some of the links in the report leading to some of my GitLab repositories might not work but otherwise it should be complete.

### Code directory
The [Code](https://github.com/shaved-ice/Dissertation-GitLabUIPackage/tree/main/Code) directory contains the final package I created for my dissertation as well as the code adapted to use it for Games 1, 2, and 3 (there are 3 versions of 2 due to there being different options of how to approach adapting that game).\
Game1 is for Game 1, Game2 is for Game 2, Game2Async is the asynchronous autonomous method using version of Game 2 (uses AutonomousDeleteExtra and AutonomousReplaceMissing), Game2CheckForChanges uses the CheckForChanges method in its implementation, and Game3 is for Game 3.

To play these games please open the chosen Game folder as usual with the Unity versions and ignore the errors about not being able to find an imported package and subsequent compilation errors during project generation.\
Once you are in the project, please go to the Window tab at the tob and go into the package manager.\
Please press the "+" button at the top left and click install package from disk.\
Please go into the GitLabUnityUIPackage folder you installed with the games and open the package.json file inside. This will install the package and allow you to play.\
Please don't forget to go into the correct game scene. The correct game scene for each game can be found described in its respective repository.
### Experiment Files
The Template Experiment files discussed in the disseration appendices can be found [here](https://github.com/shaved-ice/Dissertation-GitLabUIPackage/tree/main/ExperimentTemplate)
## Semester1Work directory
### Set Up and Links
For my project management it was agreed upon that file sharing in semester one would be done within our Teams chat if necessary.\
My supervisor and I had a shared Zotero library where we shared the research papers discovered for the paper. I have exported this and placed this into this repository for easier access. (Additional details in the "Zotero Exported" section.)\
I have also created this repository for the purpose of uploading important versions of the scripts of my prototype Unity code as described in my D1 report submission. (Additional details in the "PrototypeImplementation folder setup" section.)\
I have additionally added versions of files I have worked on during the semester that contribute to my D1 submission. (Additional details in the "Other folders" section.)

### Zotero Exported
I have exported my Zotero and placed it in this repository. 
[25-Honours-Helen-Chang-Zotero-Exported.zip](https://github.com/shaved-ice/Dissertation-GitLabUIPackage/blob/main/Semester1Work/25-Honours-Helen-Chang-Zotero-Exported.zip) is the entire Zotero library exported with no other modifications and then zipped.
The Zotero library was exported in Zotero RDF format including notes, files, and annotations.
The Zotero library is structured as follows:
- 25-Honours-Helen-Chang
    - Applicable
        - Examples of connections between coding & games
        - GitLab API PrototypePackageTesting
        - Other
        - Unity Packages  
    - Maybe Applicable
    - Not Applicable
    - Not Peer-Reviewed
    - Recommended

### Other folders
The other folders contain versions of other documents that later contributed to my D1 report:
- [ExtraSetupOrWork](https://github.com/shaved-ice/Dissertation-GitLabUIPackage/tree/main/Semester1Work/ExtraSetupOrWork) contains my excel file for my semester 1 and semester 2 gantt charts as well as the initial objective files made during the start of semester 1 which plan out some possible objectives of my project.
- [DraftExperimentForms](https://github.com/shaved-ice/Dissertation-GitLabUIPackage/tree/main/Semester1Work/DraftExperimentForms) contains versions of my draft Consent, PIS, and experiment question sheet forms.
- [PrototypeImplementation](https://github.com/shaved-ice/Dissertation-GitLabUIPackage/tree/main/Semester1Work/PrototypeImplementation) contaions Unity scripts I created in semester 1 as tests of how to send different types of web requests to GitLab.

Files that have versioning will be named with their date of creation in the format: filename_DD_MM_YYYY.\
Versioned files that have the same name and date of creation will be given version numbers in the format: D1_DD_MM_YYYY_VX.docx.




