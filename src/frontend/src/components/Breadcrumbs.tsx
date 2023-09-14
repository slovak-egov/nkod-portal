
interface IProps 
{
    items: IItem[]
}

interface IItem
{
    title: string;
    link?: string;
}

export default function Breadcrumbs(props: IProps)
{
    return <div className="govuk-breadcrumbs">
        <ol className="govuk-breadcrumbs__list">
            {props.items.map(i => <li className="govuk-breadcrumbs__list-item" key={i.title}>
                {i.link ? <a className="govuk-breadcrumbs__link" href={i.link}>{i.title}</a> : i.title}
            </li>)}
        </ol>
    </div>;
}
