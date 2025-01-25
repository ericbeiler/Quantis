import React from 'react';
import GridLayout, { Layout } from 'react-grid-layout';
import 'react-grid-layout/css/styles.css';
import 'react-resizable/css/styles.css';

const Panels: React.FC = () => {
  const layout: Layout[] = [
    { i: 'a', x: 0, y: 0, w: 2, h: 2 },
    { i: 'b', x: 2, y: 0, w: 2, h: 2 },
    { i: 'c', x: 4, y: 0, w: 2, h: 2 },
  ];

 // return (<div>Hello World!</div>);
  return (
    <GridLayout
      className="layout"
      layout={layout}
      cols={12}
      rowHeight={30}
      width={1200}
    >
      <div key="a" style={{ border: '1px solid black' }}>A</div>
      <div key="b" style={{ border: '1px solid black' }}>B</div>
      <div key="c" style={{ border: '1px solid black' }}>C</div>
    </GridLayout>
  );
};

export default Panels;
