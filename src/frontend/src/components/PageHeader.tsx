import { PropsWithChildren } from "react";

type Props = PropsWithChildren

export default function PageHeader(props: Props) {
    return <h1 className="govuk-heading-xl">{props.children}</h1>;
}
