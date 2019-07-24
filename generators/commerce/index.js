'use strict';
const chalk = require('chalk');
const uuidv4 = require('uuid/v4');

const BaseGenerator = require('../../lib/base-generator');
const solutionUtils = require('../../lib/solution-utils.js');
const utils = require('../../lib/utils.js');

const baseIgnore = require('../../config/ignore.json');
const msg = require('../../config/messages.json');
const settings = require('../../config/projectSettings.json');
const commerceVersions = require('../../config/commerce.versions.json');
const commerceSettings = require('../../config/commerce.settings.json');

module.exports = class SitecoreCommerceGenerator extends BaseGenerator {
  constructor(args, opts) {
    super(args, opts);

    const config = this.config.getAll();
    if (config) {
      this.options.solutionNameUri = config.solutionNameUri;
    }
    if (config && config.promptValues) {
      this.options.solutionName = config.promptValues.solutionName;
      this.options.sitecoreVersion = config.promptValues.sitecoreVersion;
      this.options.sitecoreUpdate = config.promptValues.sitecoreUpdate;
    }

    const sitecoreUpdate = this.options.sitecoreUpdate && this.options.sitecoreUpdate.exactVersion;
    if (!sitecoreUpdate) {
      return this._showErrorAndExit(`Sitecore initial scaffolding required.`);
    }

    this.options.compatibleCommerceVersions = commerceVersions.filter(x => x.compatibleWithSitecoreVersion.includes(sitecoreUpdate));
    if (this.options.compatibleCommerceVersions.length < 1) {
      return this._showErrorAndExit(`No Sitecore Commerce available for Sitecore ${this.options.sitecoreUpdate.exactVersion}.`);
    }

    this.option('sitecoreCommerceVersion', {
       type: String,
       required: true,
       desc: 'The version of Sitecore Commerce to use.',
       default: this.options.compatibleCommerceVersions[0].value,
    });

    this.options.serviceProxyGuidSeed = "Sitecore.Commerce.ServiceProxy";
    this.options.serviceProxyCodeGuid = utils.guid(this.options.serviceProxyGuidSeed);
  }

  async prompting() {
    let answers = await this.prompt([{
      type: 'list',
      name: 'sitecoreCommerceVersion',
      message: msg.sitecoreCommerceVersion.prompt,
      choices: this.options.compatibleCommerceVersions,
      store: true,
    }]);

    this.options = { ...this.options, ...answers };

    this.options.vagrantBoxName = (this.options.sitecoreCommerceVersion.value || this.options.sitecoreCommerceVersion).vagrantBoxName;
    this.options.commerceHostNames = commerceSettings.hostNames;
  }

  writing() {
    super._runPipeline(this.options.sitecoreCommerceVersion.exactVersion, this.destinationPath(), [
      this._copyZips,
      this._copyAll
    ]);

    this._addEngineProjectToSolutionFile();

    super._updateFileContent(`${this.destinationPath()}/Vagrantfile`, [
      c => utils.updateVagrantBoxName(c, this.options.vagrantBoxName),
      c => utils.addVagrantHostNames(c, this.options.commerceHostNames),
    ]);

    super._updateFileContent(`${this.destinationPath()}/readme.md`, [
      c => utils.updateVagrantAddBoxCommand(c, this.options.vagrantBoxName),
    ]);
  }

  /* Copy dlls without any transforms */
  _copyZips(rootPath, destinationPath) {
    super._copy(this.templatePath(`${rootPath}/**/*.zip`), destinationPath, {}, super._baseGlobOptions(), {});
  }

  /* Copy majority of files with regular template transforms */
  _copyAll(rootPath, destinationPath) {
    super._copyTpl(this.templatePath(`${rootPath}/**/*`), destinationPath, {
      solutionX: this.options.solutionName,
      solutionUriX: this.options.solutionNameUri,
      sitecoreCommerceVersion: this.options.sitecoreCommerceVersion,
      sitecoreCommerceNetFrameworkVersion: this.options.sitecoreCommerceVersion.sitecoreCommerceNetFrameworkVersion,
      exactVersion: this.options.sitecoreUpdate.exactVersion,
      majorVersion: this.options.sitecoreUpdate.majorVersion,
      kernelVersion: this.options.sitecoreUpdate.kernelVersion,
      netFrameworkVersion: this.options.sitecoreUpdate.netFrameworkVersion,
      vagrantBoxNameX: this.options.vagrantBoxName,
    }, {
      ...super._baseGlobOptions(),
      ignore: [...baseIgnore, ...[ '**/*.zip' ]]
    }, {
        preProcessPath: this._processPathSolutionToken
    });
  }

  _processPathSolutionToken(destPath) {
    return destPath.replace(/SolutionX/g, '<%= solutionX %>');
  }

  _addEngineProjectToSolutionFile() {
    const commerceFolderGuid = uuidv4();
    const destinationPath = this.destinationPath();
    const solutionName = this.options.solutionName;

    super._updateFileContent(`${destinationPath}\\src\\${solutionName}.sln`, [
      c => solutionUtils.addProject(c, {
        rootFolderName: null,
        projectFolderGuid: commerceFolderGuid,
        projectFolderName: "Commerce",
        projectName: "Sitecore.Commerce.ServiceProxy",
        projectPath: commerceSettings.serviceProxyProjectPath,
        projectGuid: this.options.serviceProxyCodeGuid,
        projectTypeGuid: settings.codeProject,
      }),
    ], {
      force: true
    });
  }

  async end() {
    console.log('');
    console.log(`The ${chalk.green.bold("Sitecore.Commerce.ServiceProxy")} module has been created and added to ${chalk.green.bold(this.options.solutionName)}`);
  }
};
