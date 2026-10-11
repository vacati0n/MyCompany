// The render script (the graphics change, decision D-002 of its design). Node runs it with exactly three arguments, from the
// graphics staging folder: this file's staged name, a mode (bundle or render) and the staged request file. It reads ONE start
// line from its standard input before it loads anything: the starter writes that line only after the process is in its job,
// so every process the renderer or the browser starts is in the job. It imports nothing but Node's file, path and module
// modules, and loads the bundler and the frame renderer through a require anchored at the installed renderer folder's package
// file; it never passes a licence key, never calls the still or media render functions and never fetches a browser.
//
// Exit codes: 0 done; 3 refused before the renderer was called (no start line, an invalid request, a missing file);
// 4 the renderer failed. Anything it writes lands in the staging folder the request names.

import fs from 'node:fs';
import path from 'node:path';
import {createRequire} from 'node:module';

const refuse = (why) => {
	process.stderr.write(`refused: ${why}\n`);
	process.exit(3);
};

const fail = (error) => {
	process.stderr.write(`renderer failed: ${error && error.message ? error.message : error}\n`);
	process.exit(4);
};

const startLine = () =>
	new Promise((resolve) => {
		let text = '';
		process.stdin.setEncoding('utf8');
		process.stdin.on('data', (chunk) => {
			text += chunk;
			const at = text.indexOf('\n');
			if (at >= 0) {
				process.stdin.pause();
				resolve(text.slice(0, at).trim());
			}
		});
		process.stdin.on('end', () => resolve(text.trim()));
	});

const required = (request, key) => {
	if (request[key] === undefined || request[key] === null || request[key] === '') {
		refuse(`the request has no ${key}`);
	}

	return request[key];
};

const existing = (file, key) => {
	if (!fs.existsSync(file)) {
		refuse(`the ${key} ${file} does not exist`);
	}

	return file;
};

const [, , mode, requestFile] = process.argv;
if (process.argv.length !== 4 || (mode !== 'bundle' && mode !== 'render')) {
	refuse('the arguments are not a mode and a request file');
}

if ((await startLine()) !== 'start') {
	refuse('no start line was read');
}

let request;
try {
	request = JSON.parse(fs.readFileSync(existing(requestFile, 'request'), 'utf8'));
} catch (error) {
	refuse(`the request cannot be read: ${error.message}`);
}

const anchor = existing(required(request, 'anchor'), 'anchor');
const load = createRequire(anchor);

if (mode === 'bundle') {
	const entry = existing(path.resolve(required(request, 'entry')), 'entry');
	const publicDir = existing(path.resolve(required(request, 'publicDir')), 'publicDir');
	const outDir = path.resolve(required(request, 'outDir'));
	const modules = existing(required(request, 'modules'), 'modules');
	const moduleList = path.resolve(required(request, 'moduleList'));
	let bundle;
	try {
		({bundle} = load('@remotion/bundler'));
	} catch (error) {
		fail(error);
	}

	const resources = [];
	try {
		await bundle({
			entryPoint: entry,
			outDir,
			publicDir,
			rootDir: path.resolve('.'),
			enableCaching: false,
			onProgress: () => undefined,
			webpackOverride: (config) => ({
				...config,
				resolve: {...config.resolve, modules: [modules, ...((config.resolve && config.resolve.modules) || ['node_modules'])]},
				resolveLoader: {...config.resolveLoader, modules: [modules, ...((config.resolveLoader && config.resolveLoader.modules) || ['node_modules'])]},
				plugins: [
					...(config.plugins || []),
					{
						apply: (compiler) =>
							compiler.hooks.done.tap('moduleList', (stats) => {
								for (const module of stats.compilation.modules) {
									if (module.resource) {
										resources.push(module.resource);
									}
								}
							}),
					},
				],
			}),
		});
	} catch (error) {
		fail(error);
	}

	fs.writeFileSync(moduleList, JSON.stringify(resources.sort()));
	process.exit(0);
}

const serveUrl = existing(path.resolve(required(request, 'bundle')), 'bundle');
const browserExecutable = existing(required(request, 'browser'), 'browser');
const specification = JSON.parse(fs.readFileSync(existing(path.resolve(required(request, 'specification')), 'specification'), 'utf8'));
const style = JSON.parse(fs.readFileSync(existing(path.resolve(required(request, 'style')), 'style'), 'utf8'));
const timing = required(request, 'timing');
const faces = required(request, 'faces');
const outputDir = path.resolve(required(request, 'output'));
const concurrency = required(request, 'concurrency');
const timeoutInMilliseconds = required(request, 'frameTimeoutMs');
const inputProps = {specification, style, timing, faces};

let renderer;
try {
	renderer = load('@remotion/renderer');
} catch (error) {
	fail(error);
}

let browser;
try {
	browser = await renderer.openBrowser('chrome', {browserExecutable, logLevel: 'error'});
	const composition = await renderer.selectComposition({
		serveUrl,
		id: 'graphic',
		inputProps,
		browserExecutable,
		puppeteerInstance: browser,
		timeoutInMilliseconds,
		logLevel: 'error',
	});
	await renderer.renderFrames({
		composition,
		serveUrl,
		inputProps,
		browserExecutable,
		puppeteerInstance: browser,
		imageFormat: 'png',
		outputDir,
		concurrency: Math.min(concurrency, composition.durationInFrames),
		timeoutInMilliseconds,
		logLevel: 'error',
		onStart: () => undefined,
		onFrameUpdate: () => undefined,
		onBrowserDownload: () => {
			throw new Error('download');
		},
	});
	await browser.close({silent: true});
} catch (error) {
	if (browser) {
		await browser.close({silent: true}).catch(() => undefined);
	}

	fail(error);
}

process.exit(0);
