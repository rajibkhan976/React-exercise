import React from "react";
import { getNodeRenderOptions } from "react-virtualized-tree/lib/selectors/nodes";
import cn from "classnames";

const ReportingPlaceExpandable = ({
    onChange,
    node,
    children,
    index,
    iconsClassNameMap = {
        expanded: "fa fa-minus-square-o",
        collapsed: "fa fa-plus-square-o",
        lastChild: ""
    }
}) => {
    const { hasChildren, isExpanded } = getNodeRenderOptions(node);
    const className = cn("expand-node", {
        [iconsClassNameMap.expanded]: hasChildren && isExpanded,
        [iconsClassNameMap.collapsed]: hasChildren && !isExpanded,
        [iconsClassNameMap.lastChild]: !hasChildren
    });

    const handleChange = () =>
        onChange({
            node: {
                ...node,
                state: {
                    ...node.state,
                    expanded: !isExpanded
                }
            },
            type: 1,
            index
        });

    return (
        <div className="inner-node" onDoubleClick={handleChange}>
            {hasChildren && (<i tabIndex={0} onKeyDown={handleChange} onClick={handleChange} className={className} style={{ float: 'left' }} />)}
            {children}
        </div>
    );
};

export default ReportingPlaceExpandable;
