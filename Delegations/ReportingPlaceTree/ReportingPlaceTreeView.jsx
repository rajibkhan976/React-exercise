import React from "react";
import PropTypes from "prop-types";
import { AutoSizer, List, CellMeasurerCache, CellMeasurer } from "react-virtualized";
import cn from "classnames";
import "react-virtualized-tree/lib/main.css";

/**
 * Fork of react-virtualized-tree
 * https://github.com/diogofcunha/react-virtualized-tree
 */
export default class Tree extends React.Component {
    constructor(props) {
        super(props);

        this._cache = new CellMeasurerCache({
            fixedWidth: true,
            fixedWHeight: true,
            defaultHeight: 24,
            minHeight: 24
        });

        this.getRowCount = this.getRowCount.bind(this);
        this.measureRowRenderer = this.measureRowRenderer.bind(this);
    }

    getRowCount() {
        const { nodes } = this.props;

        return nodes.length;
    }

    getNode(index) {
        const { nodes } = this.props;

        return nodes[index];
    }

    rowRenderer({ node, key, measure, style, NodeRenderer, index, nodes }) {
        return (
            <NodeRenderer
                key={key}
                style={{
                    ...style,
                    height: 24,
                    marginLeft: 10,
                    userSelect: "none",
                    cursor: "pointer"
                }}
                node={node}
                onChange={this.props.onChange}
                measure={measure}
                index={index}
            />
        );
    }

    measureRowRenderer(nodes) {
        return ({ key, index, style, parent }) => {
            const { children } = this.props;
            const node = this.getNode(index);

            return (
                <CellMeasurer cache={this._cache} columnIndex={0} key={key} rowIndex={index} parent={parent}>
                    {m => this.rowRenderer({ ...m, index, node, key, style, NodeRenderer: children, parent })}
                </CellMeasurer>
            );
        };
    }

    render() {
        const { nodes, width, scrollToIndex, scrollToAlignment, height } = this.props;

        return (
            <AutoSizer disableWidth={Boolean(width)}>
                {({ height: autoSizerHeight, width: autoWidth }) => {
                    const newHeight = Boolean(height) ? height : autoSizerHeight;
                    const rowCount = this.getRowCount();
                    const rowHeight = this._cache.defaultHeight;

                    // FIX: horizontal scrolling.
                    const scrollFix = newHeight > rowCount * rowHeight;

                    return (
                        <List
                            className={cn("tree-view", { "fix-x-scroll": scrollFix })}
                            deferredMeasurementCache={this._cache}
                            ref={r => (this._list = r)}
                            height={newHeight}
                            rowCount={rowCount}
                            rowHeight={rowHeight}
                            rowRenderer={this.measureRowRenderer(nodes)}
                            width={width || autoWidth}
                            scrollToIndex={scrollToIndex}
                            scrollToAlignment={scrollToAlignment}
                        />
                    );
                }}
            </AutoSizer>
        );
    }
}

Tree.propTypes = {
    children: PropTypes.func.isRequired,
    onChange: PropTypes.func.isRequired,
    width: PropTypes.number,
    height: PropTypes.oneOf([PropTypes.number, PropTypes.bool]),
    scrollToIndex: PropTypes.number,
    scrollToAlignment: PropTypes.string
};
