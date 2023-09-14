import { PropsWithChildren } from "react";

interface IProps extends PropsWithChildren
{
    
}

export default function GridRow(props: IProps)
{
    return <div className="govuk-grid-row">
        {props.children}
    </div>
}