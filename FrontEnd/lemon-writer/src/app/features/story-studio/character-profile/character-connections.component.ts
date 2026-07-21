import { ChangeDetectionStrategy, Component, OnInit, computed, inject, input, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { A11yModule } from '@angular/cdk/a11y';
import { MatIconModule } from '@angular/material/icon';
import { CharacterAttributeDto, CharacterMediaReferenceDto, CharacterTimelineReferenceDto, RelationshipDto, StoryStudioService } from '../../../core/services/story-studio.service';
import { CharacterProfileModel, GalleryPortrait } from './character-profile.model';

@Component({ selector: 'app-character-connections', standalone: true, imports: [FormsModule, A11yModule, MatIconModule], templateUrl: './character-connections.component.html', styleUrl: './character-connections.component.scss', changeDetection: ChangeDetectionStrategy.OnPush })
export class CharacterConnectionsComponent implements OnInit {
  private studio = inject(StoryStudioService);
  readonly bookId = input.required<string>(); readonly character = input.required<CharacterProfileModel>();
  readonly characters = input<CharacterProfileModel[]>([]); readonly gallery = input<GalleryPortrait[]>([]); readonly events = input<{ id: string; name: string }[]>([]);
  readonly navigateCharacter = output<string>(); readonly navigateMedia = output<string>(); readonly navigateEvent = output<string>(); readonly primaryPortrait = output<string>();
  readonly relationships = signal<RelationshipDto[]>([]); readonly media = signal<CharacterMediaReferenceDto[]>([]);
  readonly timeline = signal<CharacterTimelineReferenceDto[]>([]); readonly attributes = signal<CharacterAttributeDto[]>([]);
  readonly dialog = signal<'relationship'|'media'|'timeline'|'attribute'|null>(null); readonly error = signal(''); readonly search = signal('');
  relationshipDraft: Partial<RelationshipDto> = {}; mediaDraft = { mediaId:'', role:'Appearance reference' }; timelineDraft = { eventId:'', role:'Participant', note:'' };
  attributeDraft: Partial<CharacterAttributeDto> & { optionsText?: string } = { valueType:'ShortText', value:'', displayOrder:0 };
  editingId?: string;
  readonly visibleGallery = computed(() => { const q=this.search().toLowerCase(); return this.gallery().filter(x => !q || x.name.toLowerCase().includes(q) || x.summary?.toLowerCase().includes(q)); });
  readonly attributeGroups = computed(() => { const map=new Map<string,CharacterAttributeDto[]>(); for(const a of this.attributes()){const g=a.groupName||'Other';map.set(g,[...(map.get(g)||[]),a]);} return [...map.entries()]; });

  ngOnInit():void { this.reload(); }
  reload():void { const b=this.bookId(),c=this.character().id; this.studio.relationships(b).subscribe(x=>this.relationships.set(x.filter(r=>r.from===c||r.to===c))); this.studio.characterMedia(b,c).subscribe(x=>this.media.set(x)); this.studio.characterTimeline(b,c).subscribe(x=>this.timeline.set(x)); this.studio.characterAttributes(b,c).subscribe(x=>this.attributes.set(x)); }
  other(r:RelationshipDto):CharacterProfileModel|undefined{return this.characters().find(x=>x.id===(r.from===this.character().id?r.to:r.from));}
  direction(r:RelationshipDto):string{return r.from===this.character().id?'toward':'from';}
  openRelationship(r?:RelationshipDto):void{this.editingId=r?.id;this.relationshipDraft=r?{...r}:{from:this.character().id,to:'',relationshipType:'Friend',tone:'Neutral'};this.open('relationship');}
  saveRelationship():void{const d=this.relationshipDraft;if(!d.to||!d.relationshipType)return;const call=this.editingId?this.studio.updateRelationship(this.bookId(),this.editingId,d):this.studio.createRelationship(this.bookId(),d);call.subscribe({next:x=>{this.relationships.update(a=>this.editingId?a.map(v=>v.id===x.id?x:v):[...a,x]);this.close();},error:e=>this.fail(e)});}
  removeRelationship(id:string):void{if(!confirm('Remove this relationship?'))return;this.studio.deleteRelationship(this.bookId(),id).subscribe(()=>this.relationships.update(x=>x.filter(v=>v.id!==id)));}
  openMedia(link?:CharacterMediaReferenceDto):void{this.editingId=link?.id;this.mediaDraft={mediaId:link?.mediaId||'',role:link?.role||'Appearance reference'};this.open('media');}
  saveMedia():void{if(!this.mediaDraft.mediaId)return;this.studio.saveCharacterMedia(this.bookId(),this.character().id,this.mediaDraft,this.editingId).subscribe({next:x=>{this.media.update(a=>this.editingId?a.map(v=>v.id===x.id?x:v):[...a,x]);this.close();},error:e=>this.fail(e)});}
  unlinkMedia(id:string):void{this.studio.removeCharacterMedia(this.bookId(),this.character().id,id).subscribe(()=>this.media.update(x=>x.filter(v=>v.id!==id)));}
  openTimeline(link?:CharacterTimelineReferenceDto):void{this.editingId=link?.id||undefined;this.timelineDraft={eventId:link?.eventId||'',role:link?.role||'Participant',note:link?.note||''};this.open('timeline');}
  saveTimeline():void{if(!this.timelineDraft.eventId)return;this.studio.saveCharacterTimeline(this.bookId(),this.character().id,this.timelineDraft,this.editingId).subscribe({next:x=>{this.timeline.update(a=>this.editingId?a.map(v=>v.id===x.id?x:v):[...a,x].sort((u,v)=>u.sortOrder-v.sortOrder));this.close();},error:e=>this.fail(e)});}
  unlinkTimeline(link:CharacterTimelineReferenceDto):void{if(link.legacy)return;this.studio.removeCharacterTimeline(this.bookId(),this.character().id,link.id).subscribe(()=>this.timeline.update(x=>x.filter(v=>v.id!==link.id)));}
  openAttribute(a?:CharacterAttributeDto):void{this.editingId=a?.id;this.attributeDraft=a?{...a,optionsText:a.options.join(', ')}:{valueType:'ShortText',value:'',displayOrder:this.attributes().length};this.open('attribute');}
  saveAttribute():void{const d=this.attributeDraft;if(!d.label||!d.valueType)return;const value={...d,options:d.valueType==='SingleSelect'?(d.optionsText||'').split(',').map(x=>x.trim()).filter(Boolean):[]};this.studio.saveCharacterAttribute(this.bookId(),this.character().id,value,this.editingId).subscribe({next:x=>{this.attributes.update(a=>this.editingId?a.map(v=>v.id===x.id?x:v):[...a,x]);this.close();},error:e=>this.fail(e)});}
  removeAttribute(id:string):void{if(!confirm('Delete this custom attribute?'))return;this.studio.removeCharacterAttribute(this.bookId(),this.character().id,id).subscribe(()=>this.attributes.update(x=>x.filter(v=>v.id!==id)));}
  isLinked(id:string):boolean{return this.media().some(x=>x.mediaId===id);}
  setPrimary(id:string):void{this.primaryPortrait.emit(id);}
  open(kind:'relationship'|'media'|'timeline'|'attribute'):void{this.error.set('');this.dialog.set(kind);}
  close():void{this.dialog.set(null);this.editingId=undefined;}
  private fail(error:unknown):void{const response=error as {error?:{message?:string}};this.error.set(response.error?.message||'This change could not be saved.');}
}
