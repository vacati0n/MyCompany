// The drawing program (the graphics change, decision D-001 of its design): one root registering one generic composition that
// takes a graphic's specification, the company style, the timing and the staged face files as input properties. It holds no
// subject term, no position and no text to show: every drawn character comes from the specification as a text node, in the
// one declared family, loaded from the bundle's own copy of the verified faces with no fallback family; a frame waits for
// both faces and fails if either reports anything but loaded.

import React, {useEffect, useState} from 'react';
import {Composition, cancelRender, continueRender, delayRender, registerRoot, staticFile, useCurrentFrame} from 'remotion';
import {Element} from './elements.jsx';
import {need, progressAt, stateAt} from './motion.js';

const useFaces = (family, faces) => {
	const [handle] = useState(() => delayRender());
	useEffect(() => {
		const regular = new FontFace(family, `url(${staticFile(need(faces, 'regular'))})`, {style: 'normal', weight: '400'});
		const italic = new FontFace(family, `url(${staticFile(need(faces, 'italic'))})`, {style: 'italic', weight: '400'});
		Promise.all([regular.load(), italic.load()])
			.then((loaded) => {
				if (loaded.some((face) => face.status !== 'loaded')) {
					cancelRender(new Error(family));
					return;
				}

				loaded.forEach((face) => document.fonts.add(face));
				continueRender(handle);
			})
			.catch((error) => cancelRender(error));
	}, [family, faces, handle]);
};

const Graphic = ({specification, style, timing, faces}) => {
	const family = need(style, 'family');
	useFaces(family, faces);
	const frame = useCurrentFrame();
	const progress = progressAt(frame, timing, style);
	const stateOf = stateAt(specification, progress);
	const shared = specification.shared || {};
	const value = (field) => (field !== null && typeof field === 'object' ? need(shared, need(field, 'shared')) : field);
	const elements = need(specification, 'elements');
	const find = (id) => elements.find((e) => e.id === id);
	const colours = need(style, 'colours');
	const [width, height] = [need(timing, 'width'), need(timing, 'height')];
	const margin = need(style, 'margin');

	return (
		<svg width={width} height={height} viewBox={`0 0 ${width} ${height}`} style={{background: need(colours, 'background'), fontFamily: family}}>
			<rect x={0} y={0} width={width} height={height} fill={need(colours, 'background')} />
			{elements.map((element) => (
				<Element key={need(element, 'id')} element={element} state={stateOf(element.id)} value={value} style={style} family={family} find={find} />
			))}
			{specification.disclosure ? (
				<text x={width - margin} y={height - margin} textAnchor="end" fontFamily={family} fontSize={need(need(style, 'sizes'), 'small')} fill={need(colours, 'secondary')}>
					{need(specification.disclosure, 'text')}
				</text>
			) : null}
		</svg>
	);
};

registerRoot(() => (
	<Composition
		id="graphic"
		component={Graphic}
		durationInFrames={1}
		fps={1}
		width={1}
		height={1}
		calculateMetadata={({props}) => ({
			durationInFrames: need(props.timing, 'frames'),
			fps: need(props.timing, 'fps'),
			width: need(props.timing, 'width'),
			height: need(props.timing, 'height'),
		})}
	/>
));
