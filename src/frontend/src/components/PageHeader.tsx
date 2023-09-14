import { PropsWithChildren } from "react";

interface IProps extends PropsWithChildren
{

}

export default function PageHeader(props: IProps) {
    return <h1 className="govuk-heading-xl">{props.children}</h1>;
}
