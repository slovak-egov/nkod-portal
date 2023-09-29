import { Link } from "react-router-dom";

type Props =
{
    items: IItem[]
}

type IItem =
{
    title: string;
    link?: string;
}

export default function Breadcrumbs(props: Props)
{
    return <div className="govuk-breadcrumbs">
        <ol className="govuk-breadcrumbs__list">
            {props.items.map(i => <li className="govuk-breadcrumbs__list-item" key={i.title}>
                {i.link ? <Link className="govuk-breadcrumbs__link" to={i.link}>{i.title}</Link> : i.title}
            </li>)}
        </ol>
    </div>;
}
