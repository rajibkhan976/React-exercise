import React from "react";
import cn from "classnames";

const ReportingPlaceSelection = ({ node, children, onChange, index, p }) => {
    const { state: { selected, disabled, active } = {} } = node;
    const className = cn("select-node", {
        "fa fa-check-square-o": selected,
        "fa fa-square-o": !selected,
        "is-disabled": disabled
    });

    const handleChange = () =>
        onChange({
            node: {
                ...node,
                state: {
                    ...(node.state || {}),
                    selected: !selected
                }
            },
            type: 2,
            index
        });

    return (
        <React.Fragment>
            <div className="inner-node-left-content col-xs-6-5 col-xs-offset-0-5 no-padding" style={{ width: '100%', float: 'none' }}>
                <span>
                    <i className={className} onClick={disabled ? undefined : handleChange} />
                    <span title={node.text}>{node.text}</span>
                </span>
            </div>
            {children}
        </React.Fragment>
    );
};

export default ReportingPlaceSelection;
