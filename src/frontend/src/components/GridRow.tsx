import { PropsWithChildren } from "react";

type Props = PropsWithChildren

export default function GridRow(props: Props)
{
    return <div className="govuk-grid-row">
        {props.children}
    </div>
}