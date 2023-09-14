import { HTMLAttributes } from "react";

interface IProps extends HTMLAttributes<HTMLHeadingElement>
{

}

export default function PageSubheader(props: IProps) {
    return <h2 className="govuk-heading-m" {...props}>{props.children}</h2>;
}
